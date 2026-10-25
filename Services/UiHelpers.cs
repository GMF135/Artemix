namespace Artemix.Services;

/// <summary>Petits composants construits en code (puces, cartes de catégories).</summary>
public static class UiHelpers
{
	public static Color C(string key) => (Color)Application.Current!.Resources[key];

	public static Border MakeChip(string text, bool selected, Action onTap, double minWidth = 0)
	{
		var label = new Label
		{
			Text = text,
			FontSize = 12,
			FontAttributes = FontAttributes.Bold,
			TextColor = selected ? Colors.White : C("Ink"),
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
		};

		var chip = new Border
		{
			Content = label,
			HeightRequest = 34,
			MinimumWidthRequest = minWidth,
			Padding = new Thickness(16, 0),
			StrokeThickness = 1,
			Stroke = new SolidColorBrush(C("Line")),
			BackgroundColor = selected ? C("Teal") : Colors.White,
			StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 17 },
		};

		var tap = new TapGestureRecognizer();
		tap.Tapped += (_, _) => onTap();
		chip.GestureRecognizers.Add(tap);
		return chip;
	}

	public static Border MakeCategoryCard(Models.CategoryInfo cat, int count, Action onTap)
	{
		var icon = new Border
		{
			WidthRequest = 28,
			HeightRequest = 28,
			StrokeThickness = 0,
			BackgroundColor = Color.FromArgb(cat.ColorHex),
			HorizontalOptions = LayoutOptions.Start,
			StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
		};

		var bottom = new Grid
		{
			ColumnDefinitions = new ColumnDefinitionCollection
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto),
			},
			VerticalOptions = LayoutOptions.End,
		};
		bottom.Add(new Label { Text = cat.Name, FontSize = 14, FontAttributes = FontAttributes.Bold }, 0, 0);
		bottom.Add(new Label { Text = count.ToString(), FontSize = 13, TextColor = C("Muted") }, 1, 0);

		var inner = new Grid
		{
			RowDefinitions = new RowDefinitionCollection
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
			},
			Padding = new Thickness(16, 14),
		};
		inner.Add(icon, 0, 0);
		inner.Add(bottom, 0, 1);

		var card = new Border
		{
			Style = (Style)Application.Current!.Resources["Card"],
			HeightRequest = 84,
			Content = inner,
		};

		var tap = new TapGestureRecognizer();
		tap.Tapped += (_, _) => onTap();
		card.GestureRecognizers.Add(tap);
		return card;
	}
}
