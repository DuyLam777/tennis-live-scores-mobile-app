using TennisApp.Views;

namespace TennisApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(BluetoothConnectionPage), typeof(BluetoothConnectionPage));
	}
}
