using Artemix.Models;
using Artemix.Services;

namespace Artemix.Pages;

public static class SearchState
{
	public static string? PendingQuery { get; set; }
}

public partial class HomePage : ContentPage
{
	private readonly DocumentService _docs;

	public HomePage(DocumentService docs)
	{
		InitializeComponent();
		_docs = docs;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		SearchEntry.Text = "";
		BuildCategories();

		var recents = _docs.Recent(5);
		BindableLayout.SetItemsSource(RecentList, recents);
		EmptyLabel.IsVisible = recents.Count == 0;
	}

	private void BuildCategories()
	{
		CategoryGrid.Children.Clear();
		CategoryGrid.RowDefinitions.Clear();

		for (var i = 0; i < Categories.All.Count; i++)
		{
			if (i % 2 == 0) CategoryGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

			var cat = Categories.All[i];
			var card = UiHelpers.MakeCategoryCard(cat, _docs.Count(cat.Name), async () =>
				await Shell.Current.GoToAsync($"category?name={Uri.EscapeDataString(cat.Name)}"));
			CategoryGrid.Add(card, i % 2, i / 2);
		}
	}

	private async void OnSearchCompleted(object? sender, EventArgs e)
	{
		SearchState.PendingQuery = SearchEntry.Text;
		await Shell.Current.GoToAsync("//SearchPage");
	}

	private async void OnAddTapped(object? sender, TappedEventArgs e) =>
		await Shell.Current.GoToAsync("import");
}
