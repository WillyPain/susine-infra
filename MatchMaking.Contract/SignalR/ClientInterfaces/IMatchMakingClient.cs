namespace MatchMaking.Contract.SignalR.ClientInterfaces
{
    public interface IMatchMakingClient
    {
        Task JoinedQueue();
        Task MatchFound(Guid matchId, string ipAddress, int tcpPort, int udpPort);
    }
}
