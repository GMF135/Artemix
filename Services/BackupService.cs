using System.IO.Compression;
using System.Text.Json;
using Artemix.Models;
using Microsoft.Maui.Storage;

namespace Artemix.Services;

/// <summary>Sauvegarde = archive ZIP (documents.json + dossier vault). Restauration = remplacement des documents actuels.</summary>
public class BackupService
{
	private readonly DocumentService _docs;
	private CancellationTokenSource? _autoCts;

	public BackupService(DocumentService docs)
	{
		_docs = docs;
		_docs.Changed += OnDocumentsChanged;
	}

	public static string BackupDir
	{
		get
		{
			var dir = Path.Combine(FileSystem.AppDataDirectory, "backups");
			Directory.CreateDirectory(dir);
			return dir;
		}
	}

	public bool HasBackup => AppSettings.LastBackupPath.Length > 0 && File.Exists(AppSettings.LastBackupPath);

	public async Task<string> CreateAsync()
	{
		var json = _docs.ExportIndexJson();
		var count = _docs.Count();
		var bytes = _docs.TotalBytes();
		var file = Path.Combine(BackupDir, $"artemix_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

		await Task.Run(() =>
		{
			using (var zip = ZipFile.Open(file, ZipArchiveMode.Create))
			{
				var entry = zip.CreateEntry("documents.json");
				using (var w = new StreamWriter(entry.Open()))
					w.Write(json);

				if (Directory.Exists(DocumentService.VaultPath))
					foreach (var f in Directory.GetFiles(DocumentService.VaultPath))
						zip.CreateEntryFromFile(f, "vault/" + Path.GetFileName(f));
			}
		});

		AppSettings.LastBackupTicks = DateTime.Now.Ticks;
		AppSettings.LastBackupCount = count;
		AppSettings.LastBackupBytes = bytes;
		AppSettings.LastBackupPath = file;
		Prune();
		return file;
	}

	/// <summary>Remplace tous les documents actuels par ceux de l'archive.</summary>
	public async Task RestoreAsync(string zipPath)
	{
		await Task.Run(() =>
		{
			var tmp = Path.Combine(FileSystem.CacheDirectory, "restore_" + Guid.NewGuid().ToString("N"));
			try
			{
				ZipFile.ExtractToDirectory(zipPath, tmp);

				var indexFile = Path.Combine(tmp, "documents.json");
				if (!File.Exists(indexFile))
					throw new InvalidDataException("Cette archive n'est pas une sauvegarde Artemix.");

				var items = JsonSerializer.Deserialize<List<DocumentItem>>(File.ReadAllText(indexFile))
				            ?? throw new InvalidDataException("Index de sauvegarde illisible.");

				var srcVault = Path.Combine(tmp, "vault");
				items = items.Where(i =>
					i.StoredFileName.Length > 0 &&
					Path.GetFileName(i.StoredFileName) == i.StoredFileName &&
					File.Exists(Path.Combine(srcVault, i.StoredFileName))).ToList();

				if (Directory.Exists(DocumentService.VaultPath))
					Directory.Delete(DocumentService.VaultPath, true);
				Directory.CreateDirectory(DocumentService.VaultPath);

				foreach (var i in items)
					File.Copy(Path.Combine(srcVault, i.StoredFileName), Path.Combine(DocumentService.VaultPath, i.StoredFileName), true);

				File.WriteAllText(_docs.IndexPath, JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true }));
			}
			finally
			{
				try { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); } catch { }
			}
		});

		_docs.Reload();
	}

	private void Prune()
	{
		try
		{
			foreach (var old in Directory.GetFiles(BackupDir, "artemix_*.zip")
				         .OrderByDescending(f => f).Skip(5))
				File.Delete(old);
		}
		catch { }
	}

	private void OnDocumentsChanged()
	{
		if (!AppSettings.AutoBackup) return;

		_autoCts?.Cancel();
		var cts = _autoCts = new CancellationTokenSource();
		_ = Task.Run(async () =>
		{
			try
			{
				await Task.Delay(3000, cts.Token);
				await CreateAsync();
			}
			catch (OperationCanceledException) { }
			catch { }
		});
	}
}
