using GameServerOrchestrator.Client;
using MatchMaking.Api.Data.Redis;
using MatchMaking.Contract.SignalR.ClientInterfaces;
using MatchMaking.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MatchMaking.Server.Services
{
    public class MatchMakingService(IHubContext<MatchMakingHub, 
        IMatchMakingClient> _context,
        IGsoClient _gsoClient,
        MatchMakingQueue _queue)
    {
        public readonly int LobbySize = 1;
        public async Task Enqueue(string connectionId, Guid userId)
        {
            long queueSize = await _queue.Count();
            if (queueSize + 1 >= LobbySize)
            {
                // Pop N players atomically
                var result = await _queue.PopQueue(LobbySize - 1);
                var groupId = Guid.NewGuid();
                foreach (var playerConnectionId in result.Select(ticket => ticket.ConnectionId))
                {
                    await _context.Groups.AddToGroupAsync(playerConnectionId, groupId.ToString());
                }
                // Add the connecting player to the group as well
                await _context.Groups.AddToGroupAsync(connectionId, groupId.ToString());

                //Need to send request to GSO
                var gameServer = await _gsoClient.Register(matchId: groupId);

                await _context.Clients.Group(groupId.ToString())
                    .MatchFound(groupId, gameServer.IpAddress, gameServer.TcpPort, gameServer.UdpPort);
            }
            else
            {
                // TODO: make some constants for the magic strings
                await _queue.Enqueue(userId, connectionId);
                await _context.Clients.Client(connectionId).JoinedQueue();
            }
        }

        public async Task Dequeue(Guid userId, string connectionId)
        {
            await _queue.RemoveById(userId, connectionId);
        }
    }
}
