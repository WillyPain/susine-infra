using Duende.IdentityModel.OidcClient;
using MatchMaking.Contract;
using MatchMaking.Contract.SignalR.ClientInterfaces;
using Microsoft.AspNetCore.SignalR.Client;

namespace MatchMaking.Client.HubConnections
{
    public class MatchMakingHubConnection(IHubConnectionBuilder builder, OidcClient oidcClient) : IAsyncDisposable, IMatchMakingClient
    {
        private HubConnection? _hubConnection;

        public event Action OnQueueJoined;
        public event Action OnMatchJoined;

        private static string? AccessToken => LoginPage.CurrentAccessToken;

        public async Task StartAsync()
        {
            _hubConnection = new HubConnectionBuilder().WithUrl(new Uri($"{Definitions.Domain}{Definitions.Hubs.MatchMakingEndpoint}"), options =>
            {
                //todo: just testing, get an auth service in here!
                options.AccessTokenProvider = () => Task.FromResult(AccessToken);
            })
            .WithAutomaticReconnect()
            .Build();

            _hubConnection.On(nameof(JoinedQueue), JoinedQueue);
            _hubConnection.On<Guid>(nameof(MatchFound), MatchFound);
            await _hubConnection.StartAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }

        public async Task JoinedQueue()
        {
            OnQueueJoined();
        }

        public async Task MatchFound(Guid matchId)
        {
            OnMatchJoined();
            await DisposeAsync();
        }
    }
}
