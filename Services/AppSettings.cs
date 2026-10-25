using Microsoft.Maui.Storage;

namespace Artemix.Services;

/// <summary>Réglages persistants (SharedPreferences / NSUserDefaults).</summary>
public static class AppSettings
{
	private static IPreferences P => Preferences.Default;

	public static bool BiometricEnabled { get => P.Get("bio", false); set => P.Set("bio", value); }
	public static bool HideInRecents { get => P.Get("hide_recents", true); set => P.Set("hide_recents", value); }
	public static bool AutoBackup { get => P.Get("auto_backup", false); set => P.Set("auto_backup", value); }

	/// <summary>0 = immédiat, -1 = jamais, sinon délai en secondes.</summary>
	public static int AutoLockSeconds { get => P.Get("autolock", 60); set => P.Set("autolock", value); }

	public static long LastBackupTicks { get => P.Get("lb_ticks", 0L); set => P.Set("lb_ticks", value); }
	public static int LastBackupCount { get => P.Get("lb_count", 0); set => P.Set("lb_count", value); }
	public static long LastBackupBytes { get => P.Get("lb_bytes", 0L); set => P.Set("lb_bytes", value); }
	public static string LastBackupPath { get => P.Get("lb_path", ""); set => P.Set("lb_path", value); }

	public static readonly (string Label, int Seconds)[] AutoLockOptions =
	{
		("Immédiatement", 0),
		("30 s", 30),
		("1 min", 60),
		("5 min", 300),
		("Jamais", -1),
	};

	public static string AutoLockLabel =>
		AutoLockOptions.FirstOrDefault(o => o.Seconds == AutoLockSeconds).Label ?? "1 min";
}
