using Artemix.Pages;
using Artemix.Services;

namespace Artemix;

public partial class App : Application
{
	private readonly SecurityService _security;
	private readonly BackupService _backup; // instancié ici pour activer la sauvegarde automatique
	private Window? _window;
	private DateTime? _stoppedAt;

	public App(SecurityService security, BackupService backup)
	{
		InitializeComponent();
		_security = security;
		_backup = backup;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		_window = new Window(new SplashPage());
		_window.Stopped += (_, _) => { if (_security.IsUnlocked) _stoppedAt = DateTime.UtcNow; };
		_window.Resumed += (_, _) => OnResumedFromBackground();
		return _window;
	}

	/// <summary>Remplace la page racine (splash → PIN → application).</summary>
	public static void SetRoot(Page page)
	{
		if (Current is App app && app._window != null)
			app._window.Page = page;
	}

	private void OnResumedFromBackground()
	{
		var stoppedAt = _stoppedAt;
		_stoppedAt = null;

		if (stoppedAt == null || !_security.IsUnlocked || _security.SuspendAutoLock) return;

		var limit = AppSettings.AutoLockSeconds;
		if (limit < 0) return; // « Jamais »

		if ((DateTime.UtcNow - stoppedAt.Value).TotalSeconds >= limit)
		{
			_security.IsUnlocked = false;
			SetRoot(new PinPage(PinMode.Unlock));
		}
	}
}
