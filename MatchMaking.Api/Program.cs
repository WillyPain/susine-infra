﻿using MatchMaking.Contract;
using MatchMaking.Server.Data;
using MatchMaking.Server.Hubs;
using MatchMaking.Server.Services;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Client;
using OpenIddict.Validation;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Validation.OpenIddictValidationEvents;
using static OpenIddict.Client.OpenIddictClientHandlers.Introspection;
using static OpenIddict.Abstractions.OpenIddictConstants;
using StackExchange.Redis;
using MatchMaking.Api.Data.Redis;
using GameServerOrchestrator.Client;

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
            Issuer = new Uri("http://service-identity:8080/", UriKind.Absolute),
            ClaimsIssuer = "https://identity.susine.dev",
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
        options.SetIssuer("http://service-identity:8080/");
        options.SetClaimsIssuer("https://identity.susine.dev/");
        options.AddAudiences(clientId);

        options.AddEventHandler<HandleIntrospectionResponseContext>(builder =>
        {
            builder.SetOrder(ValidateWellKnownParameters.Descriptor.Order + 998);
            builder.SetType(OpenIddictValidationHandlerType.Custom);
            builder.UseInlineHandler(context =>
            {
                /* Linkerd doesnt let you route based off fqdn which causes some issues 
                 * with the issuer in the claims (pun not intended).*/

                // This handler:
                    // 1. check if the issuer is either the internal service name or the fqdn (either is fine)
                    // 2. If valid issuer overrite it with the internal service name so the openiddict validation succeeds
                var issuer = (string?)context.Response[Claims.Issuer];
                if (!string.IsNullOrEmpty(issuer) && (issuer.Equals("http://service-identity:8080/") || issuer.Equals("https://identity.susine.dev/")))
                {
                    context.Response[Claims.Issuer] = "http://service-identity:8080/";
                }

                return default;
            });
        });

        options.UseIntrospection()
               .SetClientId(clientId)
               .SetClientSecret(clientSecret);

        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

//TODO: gotta make this env var
var connection = ConnectionMultiplexer.Connect("redis-leader:6379");
builder.Services.AddSingleton(connection);
builder.Services.AddScoped(s =>
{
    var connection = s.GetService<ConnectionMultiplexer>();
    return connection!.GetDatabase();
});

builder.Services.AddSignalR();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();

builder.Services.AddScoped<MatchMakingQueue>();
builder.Services.AddScoped<MatchMakingService>();

// TODO: bit of an issue here (Susine.Clients need this memory cache to store access tokens)
// will need to find a way to bring this back into the Susine.Client infrastructure
builder.Services.AddMemoryCache();
builder.Services.AddGsoClient(options =>
{
    options.BaseUrl = "http://service-gso:8080";
});

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate(); // Applies any pending migrations
}

app.Run();