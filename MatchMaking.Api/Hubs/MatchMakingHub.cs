using MatchMaking.Contract.SignalR.ClientInterfaces;
using MatchMaking.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MatchMaking.Server.Hubs
{
    [Authorize]
    public class MatchMakingHub(MatchMakingService _matchMakingService) : Hub<IMatchMakingClient>
    {
        public async override Task OnConnectedAsync()
        {
            //todo fix fix fix
            if (Context.UserIdentifier == null)
            {
                throw new Exception();
            }

            //TODO: fix this
            //lets pass through connectionId for now
            await _matchMakingService.Enqueue(Context.ConnectionId, Guid.Parse(Context.UserIdentifier));

            await base.OnConnectedAsync();
        }

        public async override Task OnDisconnectedAsync(Exception? exception)
        {
            await _matchMakingService.Dequeue(Guid.Parse(Context!.UserIdentifier!), Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
