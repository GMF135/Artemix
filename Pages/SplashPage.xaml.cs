using Artemix.Services;

namespace Artemix.Pages;

public partial class SplashPage : ContentPage
{
	private bool _started;

	public SplashPage() => InitializeComponent();

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_started) return;
		_started = true;

		await Task.Delay(1400);
		var security = ServiceHelper.Get<SecurityService>();
		App.SetRoot(new PinPage(security.HasPin ? PinMode.Unlock : PinMode.Create));
	}
}
