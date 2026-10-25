using Artemix.Models;
using Artemix.Services;

namespace Artemix.Pages;

public partial class SearchPage : ContentPage
{
	private readonly DocumentService _docs;
	private string? _category; // null = « Tous »

	public SearchPage(DocumentService docs)
	{
		InitializeComponent();
		_docs = docs;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		if (SearchState.PendingQuery != null)
		{
			SearchEntry.Text = SearchState.PendingQuery;
			SearchState.PendingQuery = null;
		}

		BuildChips();
		Refresh();
	}

	private void BuildChips()
	{
		ChipsRow.Children.Clear();
		ChipsRow.Add(UiHelpers.MakeChip("Tous", _category == null, () => SelectCategory(null)));
		foreach (var cat in Categories.All)
		{
			var name = cat.Name;
			ChipsRow.Add(UiHelpers.MakeChip(name, _category == name, () => SelectCategory(name)));
		}
	}

	private void SelectCategory(string? name)
	{
		_category = name;
		BuildChips();
		Refresh();
	}

	private void Refresh()
	{
		var results = _docs.Search(SearchEntry.Text, _category);
		BindableLayout.SetItemsSource(ResultList, results);
		CountLabel.Text = Format.Plural(results.Count, "résultat", "résultats");
	}

	private void OnSearchChanged(object? sender, TextChangedEventArgs e) => Refresh();
}
