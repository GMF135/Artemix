using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using Artemix.Models;
using Artemix.Services;

namespace Artemix.Pages;

[QueryProperty(nameof(PresetCategory), "category")]
public partial class ImportPage : ContentPage
{
	private readonly DocumentService _docs;
	private readonly SecurityService _security;

	private string _category = Categories.Other;
	private string? _tempPath;
	private string? _originalName;
	private long _size;

	public string PresetCategory
	{
		set
		{
			if (!string.IsNullOrWhiteSpace(value)) _category = value;
		}
	}

	public ImportPage(DocumentService docs, SecurityService security)
	{
		InitializeComponent();
		_docs = docs;
		_security = security;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		BuildChips();
	}

	private void BuildChips()
	{
		ChipsLayout.Children.Clear();
		foreach (var cat in Categories.All)
		{
			var name = cat.Name;
			var chip = UiHelpers.MakeChip(name, name == _category, () =>
			{
				_category = name;
				BuildChips();
			});
			chip.Margin = new Thickness(0, 0, 8, 8);
			ChipsLayout.Add(chip);
		}
	}

	// ---------- Sources ----------

	private async void OnPickFile(object? sender, TappedEventArgs e)
	{
		try
		{
			var types = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
			{
				{ DevicePlatform.Android, new[] { "application/pdf", "image/*" } },
				{ DevicePlatform.iOS, new[] { "com.adobe.pdf", "public.image" } },
				{ DevicePlatform.MacCatalyst, new[] { "com.adobe.pdf", "public.image" } },
				{ DevicePlatform.WinUI, new[] { ".pdf", ".jpg", ".jpeg", ".png" } },
			});

			var result = await _security.RunSuspendedAsync(() =>
				FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choisir un document", FileTypes = types }));
			await UseFileAsync(result);
		}
		catch (Exception ex)
		{
			await DisplayAlert("Erreur", ex.Message, "OK");
		}
	}

	private async void OnPickCamera(object? sender, TappedEventArgs e)
	{
		try
		{
			if (!MediaPicker.Default.IsCaptureSupported)
			{
				await DisplayAlert("Caméra", "La caméra n’est pas disponible sur cet appareil.", "OK");
				return;
			}
			var photo = await _security.RunSuspendedAsync(() => MediaPicker.Default.CapturePhotoAsync());
			await UseFileAsync(photo);
		}
		catch (PermissionException)
		{
			await DisplayAlert("Caméra", "Autorisez l’accès à la caméra dans les réglages du téléphone.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Erreur", ex.Message, "OK");
		}
	}

	private async void OnPickGallery(object? sender, TappedEventArgs e)
	{
		try
		{
			var photo = await _security.RunSuspendedAsync(() => MediaPicker.Default.PickPhotoAsync());
			await UseFileAsync(photo);
		}
		catch (Exception ex)
		{
			await DisplayAlert("Erreur", ex.Message, "OK");
		}
	}

	private async Task UseFileAsync(FileResult? file)
	{
		if (file == null) return;

		var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
		var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif" };
		if (!allowed.Contains(ext))
		{
			await DisplayAlert("Format non pris en charge", "Choisissez un PDF ou une image (JPG, PNG…).", "OK");
			return;
		}

		var dir = Path.Combine(FileSystem.CacheDirectory, "import");
		Directory.CreateDirectory(dir);
		var path = Path.Combine(dir, Guid.NewGuid().ToString("N") + ext);

		await using (var src = await file.OpenReadAsync())
		await using (var dst = File.Create(path))
			await src.CopyToAsync(dst);

		if (_tempPath != null) TryDelete(_tempPath);
		_tempPath = path;
		_originalName = file.FileName;
		_size = new FileInfo(path).Length;

		FileLabel.Text = $"{file.FileName} · {Format.Size(_size)}";
		FileLabel.TextColor = UiHelpers.C("Ink");

		if (string.IsNullOrWhiteSpace(NameEntry.Text))
			NameEntry.Text = Path.GetFileNameWithoutExtension(file.FileName);
	}

	// ---------- Enregistrement ----------

	private async void OnSave(object? sender, EventArgs e)
	{
		if (_tempPath == null || _originalName == null)
		{
			await DisplayAlert("Fichier manquant", "Choisissez d’abord un fichier, une photo ou une image.", "OK");
			return;
		}
		if (string.IsNullOrWhiteSpace(NameEntry.Text))
		{
			await DisplayAlert("Nom manquant", "Donnez un nom à votre document.", "OK");
			return;
		}

		SaveButton.IsEnabled = false;
		try
		{
			await _docs.AddAsync(NameEntry.Text!, _category, _tempPath, _originalName);
			TryDelete(_tempPath);
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Erreur", ex.Message, "OK");
		}
		finally
		{
			SaveButton.IsEnabled = true;
		}
	}

	private async void OnBackTapped(object? sender, TappedEventArgs e)
	{
		if (_tempPath != null) TryDelete(_tempPath);
		await Shell.Current.GoToAsync("..");
	}

	private static void TryDelete(string path)
	{
		try { if (File.Exists(path)) File.Delete(path); } catch { }
	}
}
