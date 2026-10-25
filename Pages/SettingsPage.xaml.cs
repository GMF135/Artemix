using Microsoft.Maui.ApplicationModel;
using Artemix.Services;

namespace Artemix.Pages;

public partial class SettingsPage : ContentPage
{
	private readonly IBiometricService _bio;
	private readonly SecurityService _security;
	private bool _loading;

	public SettingsPage(IBiometricService bio, SecurityService security)
	{
		InitializeComponent();
		_bio = bio;
		_security = security;
		VersionLabel.Text = $"Version {AppInfo.Current.VersionString}";
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_loading = true;
		BioSwitch.IsToggled = AppSettings.BiometricEnabled;
		BioSwitch.IsEnabled = _bio.IsSupported();
		HideSwitch.IsToggled = AppSettings.HideInRecents;
		AutoLockLabel.Text = AppSettings.AutoLockLabel;
		_loading = false;
	}

	private async void OnChangePin(object? sender, TappedEventArgs e) =>
		await Navigation.PushModalAsync(new PinPage(PinMode.Change));

	private async void OnBioToggled(object? sender, ToggledEventArgs e)
	{
		if (_loading) return;

		if (!e.Value)
		{
			AppSettings.BiometricEnabled = false;
			return;
		}

		// Active seulement si l'utilisateur prouve qu'une empreinte fonctionne
		var ok = false;
		try
		{
			ok = await _security.RunSuspendedAsync(() =>
				_bio.AuthenticateAsync("Artemix", "Confirmez avec votre empreinte"));
		}
		catch { }

		if (ok)
		{
			AppSettings.BiometricEnabled = true;
		}
		else
		{
			_loading = true;
			BioSwitch.IsToggled = false;
			_loading = false;
			await DisplayAlert("Empreinte digitale",
				"Impossible d’activer l’empreinte. Vérifiez qu’une empreinte est enregistrée dans les réglages du téléphone.", "OK");
		}
	}

	private async void OnAutoLockTapped(object? sender, TappedEventArgs e)
	{
		var labels = AppSettings.AutoLockOptions.Select(o => o.Label).ToArray();
		var choice = await DisplayActionSheet("Verrouillage automatique", "Annuler", null, labels);
		var picked = AppSettings.AutoLockOptions.FirstOrDefault(o => o.Label == choice);
		if (picked.Label == null) return;

		AppSettings.AutoLockSeconds = picked.Seconds;
		AutoLockLabel.Text = picked.Label;
	}

	private void OnHideToggled(object? sender, ToggledEventArgs e)
	{
		if (_loading) return;
		AppSettings.HideInRecents = e.Value;

#if ANDROID
		var window = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
		if (window != null)
		{
			if (e.Value) window.SetFlags(Android.Views.WindowManagerFlags.Secure, Android.Views.WindowManagerFlags.Secure);
			else window.ClearFlags(Android.Views.WindowManagerFlags.Secure);
		}
#endif
	}
}
