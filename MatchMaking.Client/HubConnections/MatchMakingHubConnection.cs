using MatchMaking.Contract;
using MatchMaking.Contract.SignalR.ClientInterfaces;
using Microsoft.AspNetCore.SignalR.Client;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
namespace MatchMaking.Client.HubConnections
{
    public class MatchMakingHubConnection : IAsyncDisposable, IMatchMakingClient
    {
        private HubConnection? _hubConnection;

        public event Action? OnQueueJoined;
        public event Action<Guid, string, int, int>? OnMatchJoined;

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
            _hubConnection.On<Guid, string, int, int>(nameof(MatchFound), MatchFound);
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
            OnQueueJoined?.Invoke();
        }

        public async Task MatchFound(Guid matchId, string ipAddress, int tcpPort, int udpPort)
        {
            OnMatchJoined?.DynamicInvoke(matchId, ipAddress, tcpPort, udpPort);
            await DisposeAsync();
        }
    }
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
