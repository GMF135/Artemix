namespace Artemix;

public interface IBiometricService
{
	bool IsSupported();
	Task<bool> AuthenticateAsync(string title, string subtitle);
}

/// <summary>Implémentation de repli (plateformes sans empreinte).</summary>
public class NoBiometricService : IBiometricService
{
	public bool IsSupported() => false;
	public Task<bool> AuthenticateAsync(string title, string subtitle) => Task.FromResult(false);
}
