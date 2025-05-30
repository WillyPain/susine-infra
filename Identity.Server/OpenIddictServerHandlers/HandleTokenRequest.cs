using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using System.Security.Claims;

namespace Identity.Server.OpenIddictServerHandlers
{
    /// <summary>
    /// Override Token request handling for ClientCredentialFlow
    /// Better than overriding the /token endpoint as we dont have to implement the handling for AuthorizationCodeFlow
    /// </summary>
    /// <param name="appManager"></param>
    /// <param name="scopeManager"></param>
    public class HandleTokenRequest(IOpenIddictApplicationManager appManager, IOpenIddictScopeManager scopeManager)
        : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
    {
        public static OpenIddictServerHandlerDescriptor Descriptor { get; }
        = OpenIddictServerHandlerDescriptor.CreateBuilder<OpenIddictServerEvents.HandleTokenRequestContext>()
            .UseScopedHandler<HandleTokenRequest>()
            .SetOrder(1)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

        public async ValueTask HandleAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
        {
            if (context?.Request.ClientId is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Request.IsClientCredentialsGrantType())
            {
                context.Options.Issuer = new UriBuilder(Uri.UriSchemeHttps, "identity.susine.dev").Uri;
                var application = await appManager.FindByClientIdAsync(context.Request.ClientId);
                if (application == null)
                {
                    throw new InvalidOperationException("The application details cannot be found in the database.");
                }

                // Create the claims-based identity that will be used by OpenIddict to generate tokens.
                var identity = new ClaimsIdentity(
                    authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                    nameType: Claims.Name,
                    roleType: Claims.Role);

                // Add the claims that will be persisted in the tokens (use the client_id as the subject identifier).
                identity.SetClaim(Claims.Subject, await appManager.GetClientIdAsync(application));
                identity.SetClaim(Claims.Name, await appManager.GetDisplayNameAsync(application));

                // Note: In the original OAuth 2.0 specification, the client credentials grant
                // doesn't return an identity token, which is an OpenID Connect concept.
                //
                // As a non-standardized extension, OpenIddict allows returning an id_token
                // to convey information about the client application when the "openid" scope
                // is granted (i.e specified when calling principal.SetScopes()). When the "openid"
                // scope is not explicitly set, no identity token is returned to the client application.

                // Set the list of scopes granted to the client application in access_token.
                identity.SetScopes(context.Request.GetScopes());
                identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());
                identity.SetDestinations(GetDestinations);

                context.SignIn(new ClaimsPrincipal(identity));
            }
        }

        //TODO: I havent read this properly, just copy pasted from docs
        static IEnumerable<string> GetDestinations(Claim claim)
        {
            // Note: by default, claims are NOT automatically included in the access and identity tokens.
            // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
            // whether they should be included in access tokens, in identity tokens or in both.

            return claim.Type switch
            {
                Claims.Name or Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],

                _ => [Destinations.AccessToken],
            };
        }
    }
}
