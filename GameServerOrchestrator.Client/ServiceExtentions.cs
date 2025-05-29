using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Susine.Client;

namespace GameServerOrchestrator.Client
{
    public static class ServiceExtentions
    {
        public static IServiceCollection AddGsoClient(this IServiceCollection services, Action<GsoClientOptions> configure)
        {
            services.TryAddSusineClient();

            services.Configure(configure);
            services.AddHttpClient<IGsoClient, GsoClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<GsoClientOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            }).AddHttpMessageHandler<AuthHeaderHandler>();

            return services;
        }
    }
}
