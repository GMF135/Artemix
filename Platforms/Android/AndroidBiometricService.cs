using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware.Biometrics;
using Android.OS;

namespace Artemix;

/// <summary>Empreinte digitale via l'API BiometricPrompt native d'Android (API 28+), sans paquet NuGet.</summary>
public class AndroidBiometricService : IBiometricService
{
	public bool IsSupported()
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(28)) return false;
		var pm = Android.App.Application.Context.PackageManager;
		return pm != null && pm.HasSystemFeature(PackageManager.FeatureFingerprint);
	}

	public Task<bool> AuthenticateAsync(string title, string subtitle)
	{
		var tcs = new TaskCompletionSource<bool>();
		var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

		if (!OperatingSystem.IsAndroidVersionAtLeast(28) || activity == null)
		{
			tcs.SetResult(false);
			return tcs.Task;
		}

		try
		{
			var executor = activity.MainExecutor;
			var builder = new BiometricPrompt.Builder(activity);
			builder.SetTitle(title);
			builder.SetSubtitle(subtitle);
			builder.SetNegativeButton("Annuler", executor, new CancelListener());
			var prompt = builder.Build();
			prompt.Authenticate(new CancellationSignal(), executor, new AuthCallback(tcs));
		}
		catch
		{
			tcs.TrySetResult(false);
		}

		return tcs.Task;
	}

	private sealed class CancelListener : Java.Lang.Object, IDialogInterfaceOnClickListener
	{
		public void OnClick(IDialogInterface? dialog, int which) { }
	}

	private sealed class AuthCallback : BiometricPrompt.AuthenticationCallback
	{
		private readonly TaskCompletionSource<bool> _tcs;
		public AuthCallback(TaskCompletionSource<bool> tcs) => _tcs = tcs;

		public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult? result)
			=> _tcs.TrySetResult(true);

		public override void OnAuthenticationError(BiometricErrorCode errorCode, Java.Lang.ICharSequence? errString)
			=> _tcs.TrySetResult(false);

		public override void OnAuthenticationFailed() { }
	}
}
