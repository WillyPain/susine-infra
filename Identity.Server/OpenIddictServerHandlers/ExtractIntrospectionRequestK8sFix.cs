using Microsoft.AspNetCore;
using OpenIddict.Server;

namespace Identity.Server.OpenIddictServerHandlers
{
    /// <summary>
    /// Set the issuer to the fqdn "identity.susine.dev" for internal servers making introspection requests.
    /// The issuer in the token must match the issuer of the introspection context
    /// </summary>
    public class ExtractIntrospectionRequestK8sFix : IOpenIddictServerHandler<OpenIddictServerEvents.ExtractIntrospectionRequestContext>
    {
        public static OpenIddictServerHandlerDescriptor Descriptor { get; }
        = OpenIddictServerHandlerDescriptor.CreateBuilder<OpenIddictServerEvents.ExtractIntrospectionRequestContext>()
            .UseScopedHandler<ExtractIntrospectionRequestK8sFix>()
            //openiddicts handler for extracting the issuer sits at 100_000 so we sneak in before
            .SetOrder(99_999) 
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

        public ValueTask HandleAsync(OpenIddictServerEvents.ExtractIntrospectionRequestContext context)
        {
            var request = context.Transaction.GetHttpRequest();
            var host = request!.Host.Host;
            if (host.Contains("service-identity"))
            {
                var issuer = new UriBuilder(Uri.UriSchemeHttps, "identity.susine.dev").Uri;
                context.Options.Issuer = issuer;
            }
            return default;
        }
    }
}
