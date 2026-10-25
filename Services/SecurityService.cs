using System.Security.Cryptography;
using System.Text;
using Microsoft.Maui.Storage;

namespace Artemix.Services;

/// <summary>Code PIN (haché + sel), état verrouillé/déverrouillé, suspension du verrouillage automatique.</summary>
public class SecurityService
{
	private const string HashKey = "pin_hash";
	private const string SaltKey = "pin_salt";

	public bool IsUnlocked { get; set; }
	public bool SuspendAutoLock { get; set; }

	public bool HasPin => Preferences.Default.ContainsKey(HashKey);

	public void SetPin(string pin)
	{
		var salt = RandomNumberGenerator.GetBytes(16);
		Preferences.Default.Set(SaltKey, Convert.ToBase64String(salt));
		Preferences.Default.Set(HashKey, Hash(pin, salt));
	}

	public bool Verify(string pin)
	{
		var saltB64 = Preferences.Default.Get(SaltKey, "");
		var stored = Preferences.Default.Get(HashKey, "");
		if (saltB64.Length == 0 || stored.Length == 0) return false;
		var computed = Hash(pin, Convert.FromBase64String(saltB64));
		return CryptographicOperations.FixedTimeEquals(
			Encoding.UTF8.GetBytes(computed), Encoding.UTF8.GetBytes(stored));
	}

	private static string Hash(string pin, byte[] salt)
	{
		var bytes = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(pin), salt, 100_000, HashAlgorithmName.SHA256, 32);
		return Convert.ToBase64String(bytes);
	}

	/// <summary>Exécute une action (sélecteur de fichier, caméra…) sans déclencher le verrouillage auto au retour.</summary>
	public async Task<T> RunSuspendedAsync<T>(Func<Task<T>> action)
	{
		SuspendAutoLock = true;
		try
		{
			return await action();
		}
		finally
		{
			await Task.Delay(1500);
			SuspendAutoLock = false;
		}
	}
}
