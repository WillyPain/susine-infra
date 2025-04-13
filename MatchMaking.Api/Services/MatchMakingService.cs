using MatchMaking.Contract.SignalR.ClientInterfaces;
using MatchMaking.Server.Data;
using MatchMaking.Server.Hubs;
using MatchMaking.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MatchMaking.Server.Services
{
    public class MatchMakingService(IHubContext<MatchMakingHub, IMatchMakingClient> _context, ApplicationDbContext _db)
    {
        public readonly int LobbySize = 2;
        public async Task Enqueue(string connectionId, Guid userId)
        {
            //todo: replace this with correct logics
            _db.QueuedPlayers.Add(new QueuedPlayer { ConnectionId = connectionId, UserId = userId });
            await _db.SaveChangesAsync();

            // cheeky match making system (obviously two players could join at once and there would be a race condition when reading the database
            // (race condition probably isnt the right word) but basically they could both read the table only has 5 records because 
            // the action of inserting and then reading the table size is not atomic
            if (_db.QueuedPlayers.Count() >= LobbySize)
            {
                var results = await _db.QueuedPlayers.Take(LobbySize).ToListAsync();
                var groupId = Guid.NewGuid();
                foreach (var player in results) {
                    await _context.Groups.AddToGroupAsync(player.ConnectionId, groupId.ToString());
                }

                _db.QueuedPlayers.RemoveRange(results);
                await _context.Clients.Group(groupId.ToString()).MatchFound(groupId);
            }
            
            await _context.Clients.Client(connectionId).JoinedQueue();
            await _db.SaveChangesAsync();
        }

        public async Task Dequeue(Guid userId)
        {
            //TODO: remove the player from the match making queue
            _db.Entry(new QueuedPlayer { UserId = userId }).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
        }
    }
}
