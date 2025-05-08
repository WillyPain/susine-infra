using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Client.OpenIddictClientHandlers.Device;
using static OpenIddict.Validation.OpenIddictValidationEvents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();


// Add services to the container.
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        var clientId = "gso-api-0438a111-89e9-43b8-a1d9-f196df19d0dd";
        var clientSecret = builder.Configuration["OAuth:ClientSecret"] ?? "";

        options.SetIssuer("http://service-identity:8080/");
        options.SetClaimsIssuer("https://identity.susine.dev/");
        options.UseIntrospection()
               .SetClientId(clientId)
               .SetClientSecret(clientSecret);

        options.AddEventHandler<HandleIntrospectionResponseContext>(builder =>
        {
            builder.SetOrder(ValidateWellKnownParameters.Descriptor.Order + 998);
            builder.SetType(OpenIddictValidationHandlerType.Custom);

            // copy pasted from matchmaking server fix (TODO: make this shared)
            builder.UseInlineHandler(context =>
            {
                var issuer = (string?)context.Response[Claims.Issuer];
                if (!string.IsNullOrEmpty(issuer) && (issuer.Equals("http://service-identity:8080/") || issuer.Equals("https://identity.susine.dev/")))
                {
                    context.Response[Claims.Issuer] = "http://service-identity:8080/";
                }

                return default;
            });
        });

        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

builder.Services.AddScoped(_ =>
{
    var config = KubernetesClientConfiguration.InClusterConfig();
    return new Kubernetes(config);
});

//builder.Services.

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("server", [Authorize] (Kubernetes client) =>
{
    var namespaces = client.CoreV1.ListNamespace();
    foreach (var ns in namespaces.Items)
    {
        Console.WriteLine(ns.Metadata.Name);
        var list = client.CoreV1.ListNamespacedPod(ns.Metadata.Name);
        foreach (var item in list.Items)
        {
            Console.WriteLine(item.Metadata.Name);
        }
    }
    var instanceId = Guid.NewGuid();
    var labels = new Dictionary<string, string> { { "game-server-id", instanceId.ToString() } };

    var pod = new V1Pod
    {
        Metadata = new V1ObjectMeta
        {
            GenerateName = "pod-game-server-",
            Labels = labels
        },
        Spec = new V1PodSpec
        {
            HostNetwork = true,
            RestartPolicy = "Never",
            Containers = [ new() {
                Name = "bright-moon",
                Image = "willypain/susine-bright-moon-server:latest",
                Ports = [
                    new V1ContainerPort { ContainerPort = 4296, Protocol = "TCP" },
                    new V1ContainerPort { ContainerPort = 4297, Protocol = "UDP" }
                ],
                Resources = {
                    Requests = new Dictionary<string,ResourceQuantity> {
                        { "cpu", new ResourceQuantity("300m") },
                    }
                }
            }],
            ImagePullSecrets = [
                new () {
                    Name = "regcred"
                }
            ]
        }
    };
    var result = client.CreateNamespacedPod(pod, "susine");
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
