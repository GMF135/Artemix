using Artemix.Services;

namespace Artemix.Pages;

public enum PinMode { Create, Unlock, Change }

public partial class PinPage : ContentPage
{
	private readonly PinMode _mode;
	private readonly SecurityService _security = ServiceHelper.Get<SecurityService>();
	private readonly IBiometricService _bio = ServiceHelper.Get<IBiometricService>();
	private readonly List<Border> _dots = new();

	private string _entry = "";
	private string _first = "";
	private int _step; // Create: 0 saisie, 1 confirmation | Change: 0 ancien, 1 nouveau, 2 confirmation
	private bool _busy;
	private bool _bioAutoTried;

	public PinPage(PinMode mode)
	{
		InitializeComponent();
		_mode = mode;
		BuildDots();
		BuildKeypad();
		ApplyStepTexts();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (_mode == PinMode.Unlock && AppSettings.BiometricEnabled && _bio.IsSupported())
		{
			BioLabel.IsVisible = true;
			if (!_bioAutoTried)
			{
				_bioAutoTried = true;
				await Task.Delay(350);
				await TryBiometricAsync();
			}
		}
	}

	// ---------- Interface ----------

	private void BuildDots()
	{
		for (var i = 0; i < 4; i++)
		{
			var dot = new Border
			{
				WidthRequest = 18,
				HeightRequest = 18,
				StrokeThickness = 0,
				BackgroundColor = UiHelpers.C("Line"),
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 9 },
			};
			_dots.Add(dot);
			DotsRow.Add(dot);
		}
	}

	private void BuildKeypad()
	{
		string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "", "0", "⌫" };
		for (var i = 0; i < keys.Length; i++)
		{
			var k = keys[i];
			if (k.Length == 0) continue;

			var isBack = k == "⌫";
			var key = new Border
			{
				WidthRequest = 64,
				HeightRequest = 64,
				StrokeThickness = isBack ? 0 : 2,
				Stroke = new SolidColorBrush(UiHelpers.C("Line")),
				BackgroundColor = isBack ? Colors.Transparent : Colors.White,
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 32 },
				Content = new Label
				{
					Text = k,
					FontSize = isBack ? 24 : 26,
					TextColor = isBack ? UiHelpers.C("Muted") : UiHelpers.C("Ink"),
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
				},
			};

			var tap = new TapGestureRecognizer();
			tap.Tapped += (_, _) => OnKey(k);
			key.GestureRecognizers.Add(tap);
			Keypad.Add(key, i % 3, i / 3);
		}
	}

	private void ApplyStepTexts()
	{
		BackLabel.IsVisible = _mode == PinMode.Change || (_mode == PinMode.Create && _step > 0);
		LockBadge.IsVisible = _mode == PinMode.Unlock;

		switch (_mode)
		{
			case PinMode.Unlock:
				TitleLabel.Text = "Bon retour";
				SubLabel.Text = "Saisissez votre code PIN";
				break;
			case PinMode.Create:
				TitleLabel.Text = _step == 0 ? "Créez votre code PIN" : "Confirmez votre code PIN";
				SubLabel.Text = _step == 0
					? "4 chiffres pour verrouiller l’application"
					: "Saisissez à nouveau les 4 chiffres";
				break;
			case PinMode.Change:
				TitleLabel.Text = _step switch { 0 => "Code PIN actuel", 1 => "Nouveau code PIN", _ => "Confirmez le code PIN" };
				SubLabel.Text = _step switch
				{
					0 => "Saisissez votre code actuel",
					1 => "4 chiffres pour verrouiller l’application",
					_ => "Saisissez à nouveau le nouveau code",
				};
				break;
		}
	}

	private void UpdateDots()
	{
		for (var i = 0; i < 4; i++)
			_dots[i].BackgroundColor = i < _entry.Length ? UiHelpers.C("Amber") : UiHelpers.C("Line");
	}

	private async Task ShakeAsync()
	{
		foreach (var dx in new[] { -12, 12, -8, 8, 0 })
			await DotsRow.TranslateTo(dx, 0, 45);
	}

	private async Task FailAsync(string message)
	{
		ErrorLabel.Text = message;
		await ShakeAsync();
		_entry = "";
		UpdateDots();
	}

	// ---------- Saisie ----------

	private async void OnKey(string key)
	{
		if (_busy) return;
		ErrorLabel.Text = "";

		if (key == "⌫")
		{
			if (_entry.Length > 0) _entry = _entry[..^1];
			UpdateDots();
			return;
		}

		if (_entry.Length >= 4) return;
		_entry += key;
		UpdateDots();

		if (_entry.Length < 4) return;

		_busy = true;
		try
		{
			await Task.Delay(120);
			await CompleteAsync();
		}
		finally
		{
			_busy = false;
		}
	}

	private async Task CompleteAsync()
	{
		switch (_mode)
		{
			case PinMode.Unlock:
				if (_security.Verify(_entry)) EnterApp();
				else await FailAsync("Code incorrect, réessayez.");
				break;

			case PinMode.Create:
				if (_step == 0)
				{
					_first = _entry;
					_step = 1;
					_entry = "";
					UpdateDots();
					ApplyStepTexts();
				}
				else if (_entry == _first)
				{
					_security.SetPin(_entry);
					EnterApp();
				}
				else
				{
					_step = 0;
					_first = "";
					ApplyStepTexts();
					await FailAsync("Les deux codes ne correspondent pas.");
				}
				break;

			case PinMode.Change:
				if (_step == 0)
				{
					if (_security.Verify(_entry))
					{
						_step = 1;
						_entry = "";
						UpdateDots();
						ApplyStepTexts();
					}
					else await FailAsync("Code incorrect, réessayez.");
				}
				else if (_step == 1)
				{
					_first = _entry;
					_step = 2;
					_entry = "";
					UpdateDots();
					ApplyStepTexts();
				}
				else if (_entry == _first)
				{
					_security.SetPin(_entry);
					await Navigation.PopModalAsync();
					if (Shell.Current?.CurrentPage != null)
						await Shell.Current.CurrentPage.DisplayAlert("Code PIN", "Votre code PIN a été modifié.", "OK");
				}
				else
				{
					_step = 1;
					_first = "";
					ApplyStepTexts();
					await FailAsync("Les deux codes ne correspondent pas.");
				}
				break;
		}
	}

	private void EnterApp()
	{
		_security.IsUnlocked = true;
		App.SetRoot(ServiceHelper.Get<AppShell>());
	}

	// ---------- Navigation / biométrie ----------

	private async void OnBackTapped(object? sender, TappedEventArgs e)
	{
		if (_mode == PinMode.Create && _step > 0)
		{
			_step = 0;
			_first = "";
			_entry = "";
			UpdateDots();
			ApplyStepTexts();
		}
		else if (_mode == PinMode.Change)
		{
			await Navigation.PopModalAsync();
		}
	}

	private async void OnBioTapped(object? sender, TappedEventArgs e) => await TryBiometricAsync();

	private async Task TryBiometricAsync()
	{
		try
		{
			var ok = await _security.RunSuspendedAsync(() =>
				_bio.AuthenticateAsync("Artemix", "Déverrouillez avec votre empreinte"));
			if (ok) EnterApp();
		}
		catch
		{
			// l'utilisateur garde le PIN
		}
	}
}
