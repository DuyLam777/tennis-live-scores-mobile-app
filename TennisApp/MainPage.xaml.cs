namespace TennisApp;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
	}

	private void OnCounterClicked(object sender, EventArgs e)
	{
		count++;

		if (count == 1)
			CounterBtn.Text = $"Tesetsetset {count} time";
		else
			CounterBtn.Text = $"TESTESESTSET123 {count} times";

		SemanticScreenReader.Announce(CounterBtn.Text);
	}
}

