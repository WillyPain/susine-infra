using StackExchange.Redis;
using System.Text.Json;

namespace MatchMaking.Api.Data.Redis
{
    // TODO: maybe match making ticket should just have userId, 
    // we could also keep track of Ping, Time in queue, ELO 
    public readonly struct MatchMakingTicket
    {
        public Guid UserId { get; init; }
        public string ConnectionId { get; init; }
    }

    public class MatchMakingQueue(IDatabase _redis)
    {
        public async Task<long> Enqueue(Guid userId, string connectionId)
        {
            var ticket = new MatchMakingTicket { UserId = userId, ConnectionId = connectionId };
            return await _redis.ListRightPushAsync("playerQueue", JsonSerializer.Serialize(ticket));
        }

        public async Task<long> Count()
        {
            return await _redis.ListLengthAsync("playerQueue");
        }

        public async Task<long> RemoveById(Guid userId, string connectionId)
        {
            var ticket = new MatchMakingTicket { UserId = userId, ConnectionId = connectionId };
            return await _redis.ListRemoveAsync("playerQueue", JsonSerializer.Serialize(ticket), 1);
        }

        public async Task<List<MatchMakingTicket>> PopQueue(int count)
        {
            var tickets = new List<MatchMakingTicket>();

            if (count < 1)
            {
                return tickets;
            }

            var result = await _redis.ExecuteAsync("LMPOP", 1, "playerQueue", "LEFT", "COUNT", count);

            // Pretty cool => type checks the second element of the array
            if (result is [_, RedisResult list])
            {
                var items = (string[]?)list[1];
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        var ticket = JsonSerializer.Deserialize<MatchMakingTicket>(item);
                        tickets.Add(ticket);
                    }
                }
            }
            return tickets;
        }
    }
}
