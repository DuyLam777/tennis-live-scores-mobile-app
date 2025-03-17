using TennisApp.Views;

namespace TennisApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for match selection flow
        Routing.RegisterRoute("select-match", typeof(SelectMatchPage));
        Routing.RegisterRoute("enter-live-score", typeof(EnterLiveScorePage));
        Routing.RegisterRoute("bluetooth-connection", typeof(BluetoothConnectionPage));
        Routing.RegisterRoute("new-match", typeof(CreateNewMatchPage));
        Routing.RegisterRoute("websocket-test", typeof(WebSocketPage));
    }
}
