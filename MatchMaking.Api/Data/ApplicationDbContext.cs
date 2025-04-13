using MatchMaking.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchMaking.Server.Data
{
    public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<QueuedPlayer> QueuedPlayers { get; set; }
    }
}
