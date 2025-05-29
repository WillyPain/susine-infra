using Microsoft.Extensions.Caching.Memory;
using OpenIddict.Client;

namespace Susine.Client.Tokens
{
    public interface ITokenProvider
    {
        Task<string?> GetAccessToken();
    }

    public class OpenIddictTokenProvider(IMemoryCache _cache, OpenIddictClientService _openIddictClient) : ITokenProvider
    {
        public async Task<string?> GetAccessToken() {

            return await _cache.GetOrCreateAsync("openiddict_token", async entry =>
            {
                var client = (await _openIddictClient.GetClientRegistrationsAsync()).First();
                var result = await _openIddictClient.AuthenticateWithClientCredentialsAsync(new()
                {
                    Scopes = [.. client.Scopes],
                });

                var tokenExpiry = result.AccessTokenExpirationDate ?? DateTime.UtcNow;
                entry.AbsoluteExpirationRelativeToNow = tokenExpiry - DateTime.UtcNow;
                return result.AccessToken;
            });
        }
    }
}
