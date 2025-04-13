using Duende.IdentityModel.OidcClient;
using MatchMaking.Client.HubConnections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
#endif

namespace MatchMaking.Client
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

#if WINDOWS
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(windowsLifeCycleBuilder =>
                {
                    windowsLifeCycleBuilder.OnWindowCreated(window =>
                    {
                        window.ExtendsContentIntoTitleBar = false;
                        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
                        switch (appWindow.Presenter)
                        {
                            case Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter:
                                //overlappedPresenter.SetBorderAndTitleBar(true, false);
                                overlappedPresenter.IsMaximizable = false;
                                overlappedPresenter.IsResizable = false;
                                break;
                        }
                    });     
                });
            });
#endif

            builder.Services.AddScoped<IHubConnectionBuilder, HubConnectionBuilder>();
            builder.Services.AddScoped<MatchMakingHubConnection>();

            // setup OidcClient
            builder.Services.AddSingleton(new OidcClient(new()
            {
                Authority = "https://identity.susine.dev:7082",

                ClientId = "mm-client-36bbac63-6f8b-4b6a-b7cf-a73573161729",
                Scope = "openid email offline_access mm.api",
                RedirectUri = "matchmaking.client://oauth_callback",
                Browser = new MauiAuthenticationBrowser(),
            }));

            return builder.Build();
        }
    }
}
