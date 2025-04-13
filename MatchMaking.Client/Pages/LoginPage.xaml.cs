using Duende.IdentityModel.OidcClient;
using MatchMaking.Client.HubConnections;
using MatchMaking.Client.Pages;

namespace MatchMaking.Client
{
    public partial class LoginPage : ContentPage
    {
        private readonly OidcClient _client = default!;
        private readonly MatchMakingHubConnection _matchMakingHubConnection;

        // not sure how I feel about this baddy
        public static string? CurrentAccessToken;

        public static Dictionary<string, string> Claims;

        public LoginPage(OidcClient client, MatchMakingHubConnection matchMakingHubConnection)
        {
            InitializeComponent();
            _client = client;
            _matchMakingHubConnection = matchMakingHubConnection;
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            editor.Text = "Login Clicked";

            var result = await _client.LoginAsync();
            if (result.IsError)
            {
                editor.Text = "Login Clicked";
            }

            CurrentAccessToken = result.AccessToken;
            Claims = result.User.Claims.ToDictionary(c => c.Type, c => c.Value);

            Application.Current.Windows[0].Page = new MainPage(_matchMakingHubConnection);
        }
    }
}
