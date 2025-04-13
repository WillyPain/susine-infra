using System.ComponentModel.DataAnnotations;

namespace MatchMaking.Server.Models
{
    public class QueuedPlayer
    {
        // maybe would be cool to derive userId or connectionId from the other but me not know how
        [Key]
        public Guid UserId { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
    }
}
