using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace Artemix;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
	ScreenOrientation = ScreenOrientation.Portrait,
	ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
	                       ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		// « Masquer dans les apps récentes » : FLAG_SECURE (bloque aussi les captures d'écran)
		if (Microsoft.Maui.Storage.Preferences.Default.Get("hide_recents", true))
			Window?.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
	}
}
