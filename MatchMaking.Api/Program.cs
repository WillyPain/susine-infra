using MatchMaking.Contract;
using MatchMaking.Server.Data;
using MatchMaking.Server.Hubs;
using MatchMaking.Server.Services;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Client;
using OpenIddict.Validation.AspNetCore;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite($"Filename={Path.Combine(Path.GetTempPath(), "matchmaking.sqlite3")}");
});

const string clientId = "mm-api-e2b9b4a8-943b-48aa-97fa-0489c7b6bc26";
var clientSecret = builder.Configuration["OAuth:ClientSecret"] ?? "";

builder.Services.AddOpenIddict()
    .AddClient(options =>
    {
        options.AllowClientCredentialsFlow();
        options.DisableTokenStorage();
        options.UseSystemNetHttp()
            .SetProductInformation(typeof(Program).Assembly);
        options.AddRegistration(new OpenIddictClientRegistration
        {
            Issuer = new Uri("https://identity.susine.dev/", UriKind.Absolute),
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes =
            {
                "gso.api"
            }
        });
    })
    .AddValidation(options =>
    {
        options.SetIssuer("https://identity.susine.dev/");
        options.AddAudiences(clientId);

        options.UseIntrospection()
               .SetClientId(clientId)
               .SetClientSecret(clientSecret);

        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

builder.Services.AddSignalR();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();

builder.Services.AddScoped<MatchMakingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<MatchMakingHub>(Definitions.Hubs.MatchMakingEndpoint, options => {
    //options.CloseOnAuthenticationExpiration = true; TODO: look into this
});


#region TEST
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        await Task.Delay(1000);
        var token = await GetTokenAsync(scope.ServiceProvider);
        Console.WriteLine("Access token: {0}", token);
        Console.WriteLine();

        var resource = await GetResourceAsync(scope.ServiceProvider, token);
        Console.WriteLine("API response: {0}", resource);
        Console.ReadLine();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    }
}

static async Task<string> GetTokenAsync(IServiceProvider provider)
{
    var service = provider.GetRequiredService<OpenIddictClientService>();
    // GetById not working idk
    var client = (await service.GetClientRegistrationsAsync()).Where(c => c.ClientId == clientId).First();
    var result = await service.AuthenticateWithClientCredentialsAsync(new()
    {
        Scopes = [.. client.Scopes],
    });
    return result.AccessToken;
}

static async Task<string> GetResourceAsync(IServiceProvider provider, string token)
{
    using var client = provider.GetRequiredService<HttpClient>();
    using var request = new HttpRequestMessage(HttpMethod.Get, "https://gso.susine.dev/server");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    using var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode();

    return await response.Content.ReadAsStringAsync();
}

#endregion
app.Run();