using Microsoft.AspNetCore;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerHandlers.Introspection;

namespace Identity.Server.OpenIddictServerHandlers
{
    /// <summary>
    /// This handler modifies the issuer in the validation response so it matches the internal issuer host name set by k8s stuff.
    /// This class handles extracting the response for the internal clients <see cref="ExtractIntrospectionRequestK8sFix"/>
    /// </summary>
    public class ApplyIntrospectionResponseK8sFix : IOpenIddictServerHandler<OpenIddictServerEvents.ApplyIntrospectionResponseContext>
    {
        public static OpenIddictServerHandlerDescriptor Descriptor { get; } 
            = OpenIddictServerHandlerDescriptor.CreateBuilder<OpenIddictServerEvents.ApplyIntrospectionResponseContext>()
            .UseScopedHandler<ApplyIntrospectionResponseK8sFix>()
            .SetOrder(ValidateIntrospectionRequest.Descriptor.Order + 1_001)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();
        public ValueTask HandleAsync(OpenIddictServerEvents.ApplyIntrospectionResponseContext context)
        {
            var request = context.Transaction.GetHttpRequest();
            var host = request!.Host.Host;
            if (host.Contains("service-identity"))
            {
                context.Response[Claims.Issuer] = new UriBuilder(Uri.UriSchemeHttp, host, request.Host.Port!.Value).Uri.ToString();
            }

            return default;
        }
    }
}
