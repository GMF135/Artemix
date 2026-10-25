using System.Text.Json.Serialization;
using Artemix.Services;

namespace Artemix.Models;

public class DocumentItem
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	public string Name { get; set; } = "";
	public string Category { get; set; } = Categories.Other;
	public string OriginalFileName { get; set; } = "";
	public string StoredFileName { get; set; } = "";
	public long SizeBytes { get; set; }
	public DateTime AddedAt { get; set; } = DateTime.Now;

	[JsonIgnore] public string Extension => Path.GetExtension(StoredFileName).ToLowerInvariant();
	[JsonIgnore] public bool IsPdf => Extension == ".pdf";
	[JsonIgnore] public bool IsImage => Extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" or ".gif";
	[JsonIgnore] public string TypeLabel => IsPdf ? "PDF" : IsImage ? "Image" : "Fichier";
	[JsonIgnore] public string FullPath => Path.Combine(DocumentService.VaultPath, StoredFileName);

	/// <summary>« PDF · 1,2 Mo » (liste d'une catégorie)</summary>
	[JsonIgnore] public string FileSubtitle => $"{TypeLabel} · {Format.Size(SizeBytes)}";
	/// <summary>« Identité · hier » (récents)</summary>
	[JsonIgnore] public string RecentSubtitle => $"{Category} · {Format.RelativeDay(AddedAt)}";
	/// <summary>« Identité · PDF » (résultats de recherche)</summary>
	[JsonIgnore] public string TypeSubtitle => $"{Category} · {TypeLabel}";
	/// <summary>« 05 octobre 2026 »</summary>
	[JsonIgnore] public string AddedLabel => AddedAt.ToString("dd MMMM yyyy", Format.Fr);
}
