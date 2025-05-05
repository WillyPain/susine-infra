using Microsoft.AspNetCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace Identity.Server.OpenIddictServerHandlers
{
    /// <summary>
    /// Fix the endpoints in the discovery document for internal clients using service host names
    /// Set Scheme to => http (defaults to https)
    /// Set Host to => internal service name
    /// </summary>
    public class HandleConfigurationRequestK8sFix
        : IOpenIddictServerHandler<OpenIddictServerEvents.HandleConfigurationRequestContext>
    {
        public static OpenIddictServerHandlerDescriptor Descriptor { get; }
        = OpenIddictServerHandlerDescriptor.CreateBuilder<OpenIddictServerEvents.HandleConfigurationRequestContext>()
            .UseScopedHandler<HandleConfigurationRequestK8sFix>()
            .SetOrder(1)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

        public ValueTask HandleAsync(OpenIddictServerEvents.HandleConfigurationRequestContext context)
        {
            var request = context.Transaction.GetHttpRequest();
            var host = request!.Host.Host;

            // internal traffic
            if (host.Contains("service-identity"))
            {
                var port = request!.Host.Port!.Value;
                var baseUri = new UriBuilder(Uri.UriSchemeHttp, host, port);

                // There is another event handler further down the chain that will override the Issuer based on
                // whats set in this Options object
                context.Options.Issuer = baseUri.Uri;

                baseUri.Path = "authorize";
                context.AuthorizationEndpoint = baseUri.Uri;

                baseUri.Path = "introspect";
                context.IntrospectionEndpoint = baseUri.Uri;

                baseUri.Path = "token";
                context.TokenEndpoint = baseUri.Uri;

                baseUri.Path = "userinfo";
                context.UserInfoEndpoint = baseUri.Uri;

                baseUri.Path = ".well-known/jwks";
                context.JsonWebKeySetEndpoint = baseUri.Uri;
            }
            else
            {
                var baseUri = new UriBuilder(Uri.UriSchemeHttps, host);

                // Seems to be persistant between calls (probs a singleton somewhere)
                // Anyway when we get an external call we need to make this null so the other event handler correctly set the issuer
                context.Options.Issuer = null;
            }

            return default;
        }
    }
}
