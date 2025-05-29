using GameServerOrchestrator.Contract.Models;
using StackExchange.Redis;

namespace GameServerOrchestrator.Data
{
    public class GameServerRegistry(IDatabase _redis)
    {
        //todo: i think it would be nice to have a shared library for my redis stuff
        // can have shared base redismanager that forces you to implement a applicationkeyspace
        // can handle handle setting up the redis connection using a registry file
        public static readonly string ApplicationKeySpace = "gso:";
        public static readonly string GameServerKeySpace = ApplicationKeySpace + "game-server:";
        public static readonly string RegistryKeySpace = GameServerKeySpace + "registry:";

        // TODO: env variables plz
        public static readonly int[] PortRange = { 40_000, 49_000};

        public async Task<GameServer> Register(Guid matchId, string ipAddress)
        {
            var tcpPort = await GetNextPort(ipAddress);
            var udpPort = tcpPort + 1;
            await _redis.HashSetAsync(RegistryKeySpace, [
                //TODO: Make these fields constants
                new HashEntry("match-id", matchId.ToString()),
                new HashEntry("ip-address", ipAddress),
                new HashEntry("tcp-port", tcpPort),
                new HashEntry("udp-port", udpPort),
            ]);
            return new GameServer
            {
                MatchId = matchId,
                IpAddress = ipAddress,
                TcpPort = tcpPort,
                UdpPort = udpPort
            };
        }

        public async Task<int> GetNextPort(string ipAddress)
        {
            var port = await _redis.StringIncrementAsync(GameServerKeySpace + ipAddress, 2);
            if (port >= PortRange[1] || port < PortRange[0])
            {
                await _redis.StringSetAsync(GameServerKeySpace + ipAddress, PortRange[0]);
                return PortRange[0];
            }
            return (int)port;
        }

        public async Task Delete()
        {
            //TODO:
        }
    }
}
