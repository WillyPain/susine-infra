using GameServerOrchestrator.Contract.Models;
using System.Net.Http.Json;

namespace GameServerOrchestrator.Client
{
    public interface IGsoClient
    {
        // This is bad name (this is registering the gs and deploys it)
        Task<GameServer> Register(Guid matchId);
    }

    public class GsoClientOptions()
    {
        public string BaseUrl { get; set; } = "https://gso.susine.dev";
    }

    public class GsoClient (HttpClient _client) : IGsoClient
    {
        //TODO: This has pretty bad error handling (should find some examples on better response data
        public async Task<GameServer> Register(Guid matchId)
        {
            using var response = await _client.GetAsync($"/server/{matchId}");
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            return await response.Content.ReadFromJsonAsync<GameServer>();
        }
    }
}
