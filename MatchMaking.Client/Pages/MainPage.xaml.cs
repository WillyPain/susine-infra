using MatchMaking.Client.HubConnections;

#if WINDOWS
using WinUIEx;
using System.Diagnostics;
#endif

namespace MatchMaking.Client.Pages;

public partial class MainPage : ContentPage
{
    private MatchMakingHubConnection _matchMakingHubConnection;
    private IDispatcherTimer _timer;
    private TimeSpan _currentTimeInQueue;

    public MainPage(MatchMakingHubConnection matchMakingHubConnection)
	{
        _matchMakingHubConnection = matchMakingHubConnection;
        InitializeComponent();

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_,_) => {
            _currentTimeInQueue = _currentTimeInQueue.Add(TimeSpan.FromSeconds(1));
            UpdateQueueTime();
        };

        _matchMakingHubConnection.OnQueueJoined += () => _timer.Start();
        _matchMakingHubConnection.OnMatchJoined += (matchId, ipAddres, tcpPort, udpPort) =>
        {
            StopTimer();
#if WINDOWS
            LaunchGame(matchId, ipAddres, tcpPort, udpPort);
#endif
        };
    }

    public async void OnFindMatchClicked(object sender, EventArgs e)
    {
        FindMatchButton.IsVisible = false;
        TimerSection.IsVisible = true;
        await _matchMakingHubConnection.StartAsync();
    }

    public async void OnCancelSearchClicked(object sender, EventArgs e)
    {
        StopTimer();
        await _matchMakingHubConnection.DisposeAsync();
    }

    public async void StopTimer()
    {
        _timer.Stop();
        _currentTimeInQueue = new TimeSpan();
        UpdateQueueTime();
        await Dispatcher.DispatchAsync(() =>
        {
            FindMatchButton.IsVisible = true;
            TimerSection.IsVisible = false;
        });
    }

    public void UpdateQueueTime()
    {
        TimerLabel.Text = $"Seconds in Queue: {_currentTimeInQueue.ToString(@"m\:ss")}";
    }

#if WINDOWS
    public void LaunchGame(Guid matchId, string ipAddress, int tcpPort, int udpPort)
	{
        //TODO: pass in the OTP for the game lobby (if need new connection request new OTP)
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "C:\\PROJECTS\\DarkStarSingularity\\Build-2025-broken\\DarkStarSingularity.exe",
            // TODO: I might need some better evaluation here of the fields returned by the server 
            // Might be some security issues here if dangerous stuff is returned by the server
            Arguments = $"{LoginPage.CurrentAccessToken} {LoginPage.Claims["email"]} {matchId.ToString()} {ipAddress} {tcpPort} {udpPort}",
            UseShellExecute = false
        };

        var gameClientProcess = new Process{ 
            StartInfo = psi, 
            EnableRaisingEvents = true,
        };

        gameClientProcess.Exited += (sender, e) =>
        {
            RestoreWindow();
        };

        MinimizeWindow();
        gameClientProcess.Start();
    }

    //TODO: THIS IS from gpt. do some research later on better way to do this
    private static void MinimizeWindow()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var window = Application.Current?.Windows[0]?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (window != null)
            {
                window.Minimize();
            }
        });
    }
    private static void RestoreWindow()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var window = Application.Current?.Windows[0]?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (window != null)
            {
                window.Activate(); // Bring the window back
            }
        });
    }
#else
    public void LaunchGame(object sender, EventArgs e) { throw new NotImplementedException(); }
#endif

}