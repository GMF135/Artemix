using Artemix.Models;
using Artemix.Services;

namespace Artemix.Pages;

[QueryProperty(nameof(CategoryName), "name")]
public partial class CategoryPage : ContentPage
{
	private readonly DocumentService _docs;
	private string _category = Categories.Other;

	public string CategoryName
	{
		get => _category;
		set => _category = value;
	}

	public CategoryPage(DocumentService docs)
	{
		InitializeComponent();
		_docs = docs;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		TitleLabel.Text = _category;
		Refresh();
	}

	private void Refresh()
	{
		var items = _docs.Search(SearchEntry.Text, _category);
		BindableLayout.SetItemsSource(DocList, items);
		CountLabel.Text = Format.Plural(items.Count, "document", "documents");
	}

	private void OnSearchChanged(object? sender, TextChangedEventArgs e) => Refresh();

	private async void OnBackTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("..");

	private async void OnAddTapped(object? sender, TappedEventArgs e) =>
		await Shell.Current.GoToAsync($"import?category={Uri.EscapeDataString(_category)}");
}
