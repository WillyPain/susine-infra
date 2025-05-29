using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Susine.Client.Tokens;

namespace Susine.Client
{
    public static class ServiceExtentions
    {
        public static IServiceCollection TryAddSusineClient(this IServiceCollection services) {
            // Not sure if this is a bad pattern but if a service uses multiple clients this will trigger multiple times
            services.TryAddSingleton<ITokenProvider, OpenIddictTokenProvider>();
            services.TryAddTransient<AuthHeaderHandler>();
            return services;
        }
    }
}
