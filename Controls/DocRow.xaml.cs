using Artemix.Models;

namespace Artemix.Controls;

public partial class DocRow : ContentView
{
	/// <summary>"Recent" (Identité · hier), "File" (PDF · 1,2 Mo) ou "Type" (Identité · PDF).</summary>
	public string Mode { get; set; } = "Recent";

	public DocRow()
	{
		InitializeComponent();

		var tap = new TapGestureRecognizer();
		tap.Tapped += async (_, _) =>
		{
			if (BindingContext is DocumentItem d)
				await Shell.Current.GoToAsync($"detail?id={d.Id}");
		};
		GestureRecognizers.Add(tap);
	}

	protected override void OnBindingContextChanged()
	{
		base.OnBindingContextChanged();
		if (BindingContext is DocumentItem d)
			SubLabel.Text = Mode switch
			{
				"File" => d.FileSubtitle,
				"Type" => d.TypeSubtitle,
				_ => d.RecentSubtitle,
			};
	}
}
