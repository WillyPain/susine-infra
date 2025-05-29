namespace GameServerOrchestrator.Contract.Models
{
    public struct GameServer
    {
        public Guid MatchId { get; init; }
        public string IpAddress { get; init; }
        public int TcpPort { get; init; }
        public int UdpPort { get; init; }
    }
}
