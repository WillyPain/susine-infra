using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient.Browser;

namespace MatchMaking.Client
{
    internal class MauiAuthenticationBrowser : Duende.IdentityModel.OidcClient.Browser.IBrowser
    {
        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            try
            {
#if WINDOWS
                Microsoft.Windows.AppLifecycle.ActivationRegistrationManager.RegisterForProtocolActivation(
                    "matchmaking.client", 
                    "Assets\\Square150x150Logo.scale-100", 
                    "Oidc Sample App", 
                    null);
                // use webauthenticator (doesnt work on windows tho....
                var result = await WinUIEx.WebAuthenticator.AuthenticateAsync(
                    new Uri(options.StartUrl),
                    new Uri(options.EndUrl));
#else
                var result = await WebAuthenticator.Default.AuthenticateAsync(
                new Uri(options.StartUrl),
                new Uri(options.EndUrl));
#endif

                var url = new RequestUrl("matchmaking.client://oauth_callback")
                    .Create([.. result.Properties]);

                return new BrowserResult
                {
                    Response = url,
                    ResultType = BrowserResultType.Success
                };
            }
            catch (TaskCanceledException)
            {
                return new BrowserResult
                {
                    ResultType = BrowserResultType.UserCancel
                };
            }
#if WINDOWS
            finally
            {
                Microsoft.Windows.AppLifecycle.ActivationRegistrationManager.UnregisterForProtocolActivation("matchmaking.client", null);
            }
#endif
            throw new NotImplementedException();
        }
    }
}