using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();


// Add services to the container.
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        var clientId = "gso-api-0438a111-89e9-43b8-a1d9-f196df19d0dd";
        var clientSecret = builder.Configuration["OAuth:ClientSecret"] ?? "";

        options.SetIssuer("https://identity.susine.dev:7082/");
        options.UseIntrospection()
               .SetClientId(clientId)
               .SetClientSecret(clientSecret);

        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("test", [Authorize] () => "Cool Guy 😎");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
