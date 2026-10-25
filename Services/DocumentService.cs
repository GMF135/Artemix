using System.Globalization;
using System.Text;
using System.Text.Json;
using Artemix.Models;
using Microsoft.Maui.Storage;

namespace Artemix.Services;

/// <summary>Stockage local des documents : fichiers dans /vault + index documents.json (dossier privé de l'app).</summary>
public class DocumentService
{
	private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
	private readonly object _lock = new();
	private List<DocumentItem> _items = new();
	private bool _loaded;

	public static string RootPath => FileSystem.AppDataDirectory;
	public static string VaultPath => Path.Combine(RootPath, "vault");
	public string IndexPath => Path.Combine(RootPath, "documents.json");

	public event Action? Changed;

	private void EnsureLoaded()
	{
		lock (_lock)
		{
			if (_loaded) return;
			_items = ReadIndex();
			_loaded = true;
		}
	}

	private List<DocumentItem> ReadIndex()
	{
		try
		{
			if (File.Exists(IndexPath))
				return JsonSerializer.Deserialize<List<DocumentItem>>(File.ReadAllText(IndexPath)) ?? new();
		}
		catch
		{
			// index illisible : on repart d'une liste vide
		}
		return new();
	}

	/// <summary>Relit l'index depuis le disque (après une restauration).</summary>
	public void Reload()
	{
		lock (_lock) { _loaded = false; }
		Changed?.Invoke();
	}

	public IReadOnlyList<DocumentItem> GetAll()
	{
		EnsureLoaded();
		lock (_lock) return _items.OrderByDescending(d => d.AddedAt).ToList();
	}

	public DocumentItem? Get(string id)
	{
		EnsureLoaded();
		lock (_lock) return _items.FirstOrDefault(d => d.Id == id);
	}

	public IReadOnlyList<DocumentItem> Recent(int count = 5) => GetAll().Take(count).ToList();

	public int Count(string? category = null)
	{
		EnsureLoaded();
		lock (_lock) return category == null ? _items.Count : _items.Count(d => d.Category == category);
	}

	public long TotalBytes()
	{
		EnsureLoaded();
		lock (_lock) return _items.Sum(d => d.SizeBytes);
	}

	public string ExportIndexJson()
	{
		EnsureLoaded();
		lock (_lock) return JsonSerializer.Serialize(_items, JsonOpts);
	}

	/// <summary>Recherche insensible à la casse et aux accents, sur le nom, la catégorie, le type et le fichier d'origine.</summary>
	public IReadOnlyList<DocumentItem> Search(string? query, string? category = null)
	{
		var tokens = Normalize(query ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return GetAll().Where(d =>
		{
			if (!string.IsNullOrEmpty(category) && d.Category != category) return false;
			if (tokens.Length == 0) return true;
			var hay = Normalize($"{d.Name} {d.Category} {d.TypeLabel} {d.OriginalFileName}");
			return tokens.All(t => hay.Contains(t));
		}).ToList();
	}

	public static string Normalize(string s)
	{
		var decomposed = s.Normalize(NormalizationForm.FormD);
		var sb = new StringBuilder(decomposed.Length);
		foreach (var c in decomposed)
			if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
				sb.Append(c);
		return sb.ToString().ToLowerInvariant();
	}

	public async Task<DocumentItem> AddAsync(string name, string category, string sourcePath, string originalFileName)
	{
		EnsureLoaded();
		Directory.CreateDirectory(VaultPath);

		var id = Guid.NewGuid().ToString("N");
		var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
		var stored = id + ext;
		var dest = Path.Combine(VaultPath, stored);

		await using (var src = File.OpenRead(sourcePath))
		await using (var dst = File.Create(dest))
			await src.CopyToAsync(dst);

		var item = new DocumentItem
		{
			Id = id,
			Name = name.Trim(),
			Category = category,
			OriginalFileName = originalFileName,
			StoredFileName = stored,
			SizeBytes = new FileInfo(dest).Length,
			AddedAt = DateTime.Now,
		};

		lock (_lock) _items.Add(item);
		await SaveAsync();
		Changed?.Invoke();
		return item;
	}

	public async Task DeleteAsync(string id)
	{
		EnsureLoaded();
		DocumentItem? item;
		lock (_lock)
		{
			item = _items.FirstOrDefault(d => d.Id == id);
			if (item != null) _items.Remove(item);
		}
		if (item == null) return;

		try { if (File.Exists(item.FullPath)) File.Delete(item.FullPath); } catch { }
		await SaveAsync();
		Changed?.Invoke();
	}

	private async Task SaveAsync()
	{
		string json;
		lock (_lock) json = JsonSerializer.Serialize(_items, JsonOpts);
		Directory.CreateDirectory(RootPath);
		var tmp = IndexPath + ".tmp";
		await File.WriteAllTextAsync(tmp, json);
		File.Move(tmp, IndexPath, true);
	}
}
