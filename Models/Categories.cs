namespace Artemix.Models;

public record CategoryInfo(string Name, string ColorHex);

public static class Categories
{
	public const string Other = "Autres";

	public static readonly IReadOnlyList<CategoryInfo> All = new CategoryInfo[]
	{
		new("Identité", "#0F4C5C"),
		new("Santé", "#E09F3E"),
		new("Scolarité", "#0F4C5C"),
		new("Finances", "#E09F3E"),
		new("Travail", "#0F4C5C"),
		new("Autres", "#E09F3E"),
	};

	public static CategoryInfo Get(string name) =>
		All.FirstOrDefault(c => c.Name == name) ?? All[^1];
}
