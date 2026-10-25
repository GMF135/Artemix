using Artemix.Models;
using Artemix.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace Artemix.Pages;

[QueryProperty(nameof(DocId), "id")]
public partial class DetailPage : ContentPage
{
	private readonly DocumentService _docs;
	private readonly PdfService _pdf;
	private readonly SecurityService _security;
	private DocumentItem? _doc;

	public string DocId { get; set; } = "";

	public DetailPage(DocumentService docs, PdfService pdf, SecurityService security)
	{
		InitializeComponent();
		_docs = docs;
		_pdf = pdf;
		_security = security;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		_doc = _docs.Get(DocId);
		if (_doc == null)
		{
			await Shell.Current.GoToAsync("..");
			return;
		}

		TitleLabel.Text = _doc.Name;
		CategoryLabel.Text = _doc.Category;
		DateLabel.Text = _doc.AddedLabel;

		var path = _doc.FullPath;
		if (_doc.IsImage && File.Exists(path))
		{
			PreviewImage.Source = ImageSource.FromStream(() => File.OpenRead(path));
			PreviewImage.IsVisible = true;
			PlaceholderPage.IsVisible = false;
		}
		else
		{
			PreviewImage.IsVisible = false;
			PlaceholderPage.IsVisible = true;
		}
	}

	private async void OnBackTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("..");

	private async void OnOpenTapped(object? sender, TappedEventArgs e)
	{
		if (_doc == null) return;
		try
		{
			var copy = _pdf.CopyToShareDir(_doc);
			await _security.RunSuspendedAsync(async () =>
			{
				await Launcher.Default.OpenAsync(new OpenFileRequest(_doc.Name, new ReadOnlyFile(copy)));
				return true;
			});
		}
		catch (Exception ex)
		{
			await DisplayAlert("Ouverture impossible", ex.Message, "OK");
		}
	}

	private async void OnExportPdf(object? sender, EventArgs e)
	{
		if (_doc == null) return;
		ExportButton.IsEnabled = false;
		ExportButton.Text = "Export en cours…";
		try
		{
			var pdfPath = await _pdf.ExportToPdfAsync(_doc);
			await _security.RunSuspendedAsync(async () =>
			{
				await Share.Default.RequestAsync(new ShareFileRequest
				{
					Title = $"Exporter « {_doc.Name} » en PDF",
					File = new ShareFile(pdfPath),
				});
				return true;
			});
		}
		catch (Exception ex)
		{
			await DisplayAlert("Export impossible", ex.Message, "OK");
		}
		finally
		{
			ExportButton.Text = "Exporter en PDF";
			ExportButton.IsEnabled = true;
		}
	}

	private async void OnShare(object? sender, EventArgs e)
	{
		if (_doc == null) return;
		try
		{
			var copy = _pdf.CopyToShareDir(_doc);
			await _security.RunSuspendedAsync(async () =>
			{
				await Share.Default.RequestAsync(new ShareFileRequest
				{
					Title = $"Partager « {_doc.Name} »",
					File = new ShareFile(copy),
				});
				return true;
			});
		}
		catch (Exception ex)
		{
			await DisplayAlert("Partage impossible", ex.Message, "OK");
		}
	}

	private async void OnDelete(object? sender, EventArgs e)
	{
		if (_doc == null) return;
		var ok = await DisplayAlert("Supprimer ce document ?",
			$"« {_doc.Name} » sera définitivement supprimé de l’application.", "Supprimer", "Annuler");
		if (!ok) return;

		await _docs.DeleteAsync(_doc.Id);
		await Shell.Current.GoToAsync("..");
	}
}
