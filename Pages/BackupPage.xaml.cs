using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using Artemix.Models;
using Artemix.Services;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace Artemix.Pages;

public partial class BackupPage : ContentPage
{
	private readonly BackupService _backup;
	private readonly SecurityService _security;
	private bool _loading;

	public BackupPage(BackupService backup, SecurityService security)
	{
		InitializeComponent();
		_backup = backup;
		_security = security;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_loading = true;
		AutoSwitch.IsToggled = AppSettings.AutoBackup;
		_loading = false;
		RefreshInfo();
	}

	private void RefreshInfo()
	{
		if (AppSettings.LastBackupTicks == 0)
		{
			LastLabel.Text = "Aucune sauvegarde";
			StatsLabel.Text = "Appuyez sur « Sauvegarder maintenant ».";
			return;
		}

		LastLabel.Text = Format.LastBackup(new DateTime(AppSettings.LastBackupTicks));
		StatsLabel.Text =
			$"{Format.Plural(AppSettings.LastBackupCount, "document", "documents")} · {Format.Size(AppSettings.LastBackupBytes)}";
	}

	private async void OnBackupNow(object? sender, EventArgs e)
	{
		BackupButton.IsEnabled = false;
		BackupButton.Text = "Sauvegarde en cours…";
		try
		{
			var path = await _backup.CreateAsync();
			RefreshInfo();

			var export = await DisplayAlert("Sauvegarde terminée",
				"Une copie est stockée dans l’application. Voulez-vous aussi l’enregistrer à l’extérieur (Drive, Fichiers…) ?",
				"Oui", "Non");
			if (export)
			{
				var copy = Path.Combine(PdfService.ShareDir, Path.GetFileName(path));
				File.Copy(path, copy, true);
				await _security.RunSuspendedAsync(async () =>
				{
					await Share.Default.RequestAsync(new ShareFileRequest
					{
						Title = "Sauvegarde Artemix",
						File = new ShareFile(copy),
					});
					return true;
				});
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Sauvegarde impossible", ex.Message, "OK");
		}
		finally
		{
			BackupButton.Text = "Sauvegarder maintenant";
			BackupButton.IsEnabled = true;
		}
	}

	private async void OnRestoreTapped(object? sender, TappedEventArgs e)
	{
		try
		{
			string? zipPath = null;

			if (_backup.HasBackup)
			{
				var choice = await DisplayActionSheet("Restaurer depuis…", "Annuler", null,
					"Dernière sauvegarde de l’application", "Choisir un fichier .zip");
				if (choice == null || choice == "Annuler") return;
				if (choice.StartsWith("Dernière")) zipPath = AppSettings.LastBackupPath;
			}

			if (zipPath == null)
				zipPath = await PickZipAsync();
			if (zipPath == null) return;

			var ok = await DisplayAlert("Restaurer la sauvegarde ?",
				"Les documents actuels seront remplacés par ceux de la sauvegarde.", "Restaurer", "Annuler");
			if (!ok) return;

			await _backup.RestoreAsync(zipPath);
			RefreshInfo();
			await DisplayAlert("Restauration terminée", "Vos documents ont été restaurés.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Restauration impossible", ex.Message, "OK");
		}
	}

	private async Task<string?> PickZipAsync()
	{
		var types = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
		{
			{ DevicePlatform.Android, new[] { "application/zip", "application/x-zip-compressed", "application/octet-stream" } },
			{ DevicePlatform.iOS, new[] { "public.zip-archive" } },
			{ DevicePlatform.MacCatalyst, new[] { "public.zip-archive" } },
			{ DevicePlatform.WinUI, new[] { ".zip" } },
		});

		var file = await _security.RunSuspendedAsync(() =>
			FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choisir une sauvegarde Artemix", FileTypes = types }));
		if (file == null) return null;

		var dir = Path.Combine(FileSystem.CacheDirectory, "import");
		Directory.CreateDirectory(dir);
		var path = Path.Combine(dir, Guid.NewGuid().ToString("N") + ".zip");
		await using (var src = await file.OpenReadAsync())
		await using (var dst = File.Create(path))
			await src.CopyToAsync(dst);
		return path;
	}

	private void OnAutoToggled(object? sender, ToggledEventArgs e)
	{
		if (_loading) return;
		AppSettings.AutoBackup = e.Value;
	}
}
