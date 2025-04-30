using Microsoft.AspNetCore;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Server.OpenIddictServerHandlers
{
    /// <summary>
    /// This handler modifies the issuer in the validation parameters so it matches the external issuer host name "identity.susine.dev".
    /// This class handles correcting the response for the internal clients <see cref="ApplyIntrospectionResponseK8sFix"/>
    /// </summary>
    public class ExtractIntrospectionRequestK8sFix : IOpenIddictServerHandler<OpenIddictServerEvents.ExtractIntrospectionRequestContext>
    {
        public static OpenIddictServerHandlerDescriptor Descriptor { get; }
        = OpenIddictServerHandlerDescriptor.CreateBuilder<OpenIddictServerEvents.ExtractIntrospectionRequestContext>()
            .UseScopedHandler<ExtractIntrospectionRequestK8sFix>()
            .SetOrder(99_999)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

        public ValueTask HandleAsync(OpenIddictServerEvents.ExtractIntrospectionRequestContext context)
        {
            var request = context.Transaction.GetHttpRequest();
            var host = request!.Host.Host;
            if (host.Contains("service-identity"))
            {
                //TODO: see which is actually necessary
                var issuer = new UriBuilder(Uri.UriSchemeHttps, "identity.susine.dev").Uri;
                context.Options.Issuer = issuer;
                context.Options.TokenValidationParameters.ValidIssuer = issuer.ToString();
            }
            return default;
        }
    }
}
