using Identity.Server.Data;
using Identity.Server.Models;
using Identity.Server.OpenIddictServerHandlers;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Quartz;
using System.Net;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;
var builder = WebApplication.CreateBuilder(args);

#region OpenIddict Server Config

// OpenIddict offers native integration with Quartz.NET to perform scheduled tasks
// (like pruning orphaned authorizations/tokens from the database) at regular intervals.
builder.Services.AddQuartz(options =>
{
    options.UseSimpleTypeLoader();
    options.UseInMemoryStore();
});

// Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var sqlPath = $"Filename={Path.Combine(Path.GetTempPath(), "identity.server.sqlite3")}";
    // Configure the context to use sqlite.
    options.UseSqlite(sqlPath);

    // Register the entity sets needed by OpenIddict.
    // Note: use the generic overload if you need
    // to replace the default OpenIddict entities.
    options.UseOpenIddict();
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    //TODO: need to replace this with env variable
    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("10.244.0.0"), 16));
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Register the Identity services.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;  // Allow cross-origin requests
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Ensure the cookie is only sent over HTTPS
});

const string matchMakingClientClientId = "mm-client-36bbac63-6f8b-4b6a-b7cf-a73573161729";
const string matchMakingApiClientId = "mm-api-e2b9b4a8-943b-48aa-97fa-0489c7b6bc26";
const string gameServerOrchestratorApiClientId = "gso-api-0438a111-89e9-43b8-a1d9-f196df19d0dd";

const string matchMakingApiScope = "mm.api";
const string gameServerOrchestratorApiScope = "gso.api";

builder.Services.AddOpenIddict(options =>
{
    options.AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    });
    options.AddServer(options =>
    {
        options.SetIssuer(new Uri("https://identity.susine.dev"));
        options.SetAuthorizationEndpointUris("authorize")
               .SetIntrospectionEndpointUris("introspect")
               .SetTokenEndpointUris("token")
               .SetUserInfoEndpointUris("userinfo");

        options.AllowClientCredentialsFlow();

        options.AllowAuthorizationCodeFlow()
               .AllowRefreshTokenFlow();

        options.RegisterScopes(Scopes.Email, matchMakingApiScope, gameServerOrchestratorApiScope);

        options.RegisterClaims(Claims.Email);

        //TODO: replace this with legit key
        options.AddEncryptionKey(new SymmetricSecurityKey(
            Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));

        options.AddDevelopmentSigningCertificate();

        options.AddEventHandler(HandleTokenRequest.Descriptor);

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough();
    });
    options.AddValidation(options =>
    {
        // Import the configuration from the local OpenIddict server instance.
        options.UseLocalServer();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    });
});
#endregion

#region OTHER STUFF

// Add services to the container.
builder.Services.AddRazorPages();

#endregion

var app = builder.Build();

// Needed since NGINX handles the TLS termination.
app.UseForwardedHeaders();

string matchMakingApiClientSecret = builder.Configuration["ClientSecrets:MatchMaking.Api"] ?? "";
string gameServerOrchestratorApiClientSecret = builder.Configuration["ClientSecrets:GameServerOrchestrator"] ?? "";

#region CLIENT CONFIG
await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.EnsureCreatedAsync();

    await CreateApplicationsAsync();
    await CreateScopesAsync();

    async Task CreateApplicationsAsync()
    {
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await manager.FindByClientIdAsync(matchMakingClientClientId) is var mmcClient and not null)
        {
            await manager.DeleteAsync(mmcClient);
        }
        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ApplicationType = ApplicationTypes.Native,

            ClientId = matchMakingClientClientId,
            DisplayName = "Match Making Client",
            RedirectUris = { new Uri("matchmaking.client://oauth_callback") },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.EndSession,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
                Permissions.Prefixes.Scope + matchMakingApiScope,
            },
            Requirements =
            {
                Requirements.Features.ProofKeyForCodeExchange,
            },
        });

        if (await manager.FindByClientIdAsync(matchMakingApiClientId) is var mmaClient and not null)
        {
            await manager.DeleteAsync(mmaClient);
        }
        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = matchMakingApiClientId,
            ClientSecret = matchMakingApiClientSecret,
            DisplayName = "Match Making API",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.Endpoints.Introspection,
                Permissions.Prefixes.Scope + gameServerOrchestratorApiScope,
                Permissions.GrantTypes.ClientCredentials,
            }
        });

        if (await manager.FindByClientIdAsync(gameServerOrchestratorApiClientId) is var gsoClient and not null)
        {
            await manager.DeleteAsync(gsoClient);
        }
        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = gameServerOrchestratorApiClientId,
            ClientSecret = gameServerOrchestratorApiClientSecret,
            DisplayName = "Game Server Orchestrator",
            Permissions =
            {
                Permissions.Endpoints.Introspection,
            }
        });
    }

    async Task CreateScopesAsync()
    {
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        if (await manager.FindByNameAsync(matchMakingApiScope) is var mmScope and not null)
        {
            await manager.DeleteAsync(mmScope);
        }
        await manager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = matchMakingApiScope,
            Resources =
            {
                matchMakingApiClientId
            }
        });
        if (await manager.FindByNameAsync(gameServerOrchestratorApiScope) is var gsoScope and not null)
        {
            await manager.DeleteAsync(gsoScope);
        }
        await manager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = gameServerOrchestratorApiScope,
            Resources =
            {
                gameServerOrchestratorApiClientId
            }
        });
    }
}
#endregion

#region APP CONFIG
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
#endregion

#region MINIMAL APIs
app.MapMethods("authorize", [HttpMethods.Get, HttpMethods.Post], async (HttpContext context, IOpenIddictScopeManager manager) =>
{
    // Retrieve the OpenIddict server request from the HTTP context.
    var request = context.GetOpenIddictServerRequest();

    // Check if the user is authenticated
    if (!context.User.Identity?.IsAuthenticated ?? false)
    {
        return Results.Unauthorized();
    }

    // Create the claims-based identity that will be used by OpenIddict to generate tokens.
    var identity = new ClaimsIdentity(
        authenticationType: TokenValidationParameters.DefaultAuthenticationType,
        nameType: Claims.Name,
        roleType: Claims.Role);

    // TODO: load claims from user store
    identity.AddClaim(new Claim(Claims.Subject, context.User?.Identity?.Name ?? "", ClaimValueTypes.String));
    identity.AddClaim(new Claim(Claims.Name, "egg", ClaimValueTypes.String));
    identity.AddClaim(new Claim(Claims.Email, context.User?.GetClaim(ClaimTypes.Name) ?? "", ClaimValueTypes.String));
    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

    identity.SetScopes(request?.GetScopes());

    identity.SetResources(await manager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

    // Allow all claims to be added in the access tokens.
    identity.SetDestinations(claim => [Destinations.AccessToken]);

    return Results.SignIn(new ClaimsPrincipal(identity), properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
}).RequireAuthorization();
#endregion

app.Run();
