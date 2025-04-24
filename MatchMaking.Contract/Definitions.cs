using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchMaking.Contract
{
    public static class Definitions
    {
        //TODO: move some of this shit to env variables
        public const string Domain = "https://matchmaking.susine.dev";

        public static class Hubs
        {
            public const string HubEndpointBase = "/hub";
            public const string MatchMakingEndpoint = $"{HubEndpointBase}/matchmaking";
        }
    }
}
