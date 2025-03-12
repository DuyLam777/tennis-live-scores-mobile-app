using TennisApp.Views;

namespace TennisApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("new-match", typeof(CreateNewMatchPage));
        Routing.RegisterRoute("bluetooth-connection", typeof(BluetoothConnectionPage));
        Routing.RegisterRoute("websocket-test", typeof(WebSocketPage));
    }
}
