using GameServerOrchestrator.Data;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation;
using OpenIddict.Validation.AspNetCore;
using StackExchange.Redis;
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

builder.Services.AddScoped<GameServerRegistry>();

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();

//TODO: gotta make this env var
var connection = ConnectionMultiplexer.Connect("redis-leader:6379");
builder.Services.AddSingleton(connection);
builder.Services.AddScoped(s =>
{
    var connection = s.GetService<ConnectionMultiplexer>();
    return connection!.GetDatabase();
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("server/{matchId}", [Authorize] async (
    [FromServices] Kubernetes client, 
    [FromServices] GameServerRegistry registry, 
    [FromRoute] Guid matchId) =>
{
    var labels = new Dictionary<string, string> { { "game-server-id", matchId.ToString() } };

    var gameServer = await registry.Register(matchId, "192.168.1.100");

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
            Volumes = [
                new () {
                    Name = "plugins",
                    EmptyDir = new ()
                }
            ],
            InitContainers = [ new () {
                Name = "bright-moon-plugin",
                Image = "willypain/susine-brightmoon-plugin:latest",
                Command = ["/bin/sh", "-c"],
                Args = ["cp -r /app/* /shared/"],
                VolumeMounts = [ new () {
                    Name = "plugins",
                    MountPath = "/shared"
                }]
            }],
            Containers = [ new() {
                Name = "bright-moon",
                Image = "willypain/susine-darkrift-server:latest",
                Ports = [
                    new V1ContainerPort { ContainerPort = gameServer.TcpPort, Protocol = "TCP" },
                    new V1ContainerPort { ContainerPort = gameServer.UdpPort, Protocol = "UDP" }
                ],
                Env = [
                    new V1EnvVar { Name = "port", Value = $"{gameServer.TcpPort}" },
                    new V1EnvVar { Name = "udpPort", Value = $"{gameServer.UdpPort}" }
                ],
                Resources = new () {
                    Requests = new Dictionary<string,ResourceQuantity> {
                        { "cpu", new ResourceQuantity("300m") },
                    },
                    Limits = new Dictionary<string,ResourceQuantity> {
                        { "cpu", new ResourceQuantity("300m") },
                    },
                },
                VolumeMounts = [
                    new () {
                        Name = "plugins",
                        MountPath = "/app/Plugins"
                    }
                ]
            }],
            ImagePullSecrets = [
                new () {
                    Name = "regcred"
                }
            ]
        }
    };
    var result = client.CreateNamespacedPod(pod, "susine");

    return Results.Json(gameServer);
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
