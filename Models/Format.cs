using System.Globalization;

namespace Artemix.Models;

public static class Format
{
	public static readonly CultureInfo Fr = new("fr-FR");

	public static string Size(long bytes)
	{
		if (bytes < 1024 * 1024)
			return $"{Math.Max(1, (int)Math.Round(bytes / 1024.0))} Ko";
		return string.Format(Fr, "{0:0.0} Mo", bytes / 1048576.0);
	}

	public static string RelativeDay(DateTime date)
	{
		var d = date.Date;
		if (d == DateTime.Today) return "aujourd’hui";
		if (d == DateTime.Today.AddDays(-1)) return "hier";
		return date.ToString("dd MMM", Fr);
	}

	public static string LastBackup(DateTime date)
	{
		var d = date.Date;
		var time = date.ToString("HH:mm", Fr);
		if (d == DateTime.Today) return $"Aujourd’hui, {time}";
		if (d == DateTime.Today.AddDays(-1)) return $"Hier, {time}";
		return date.ToString("dd MMM yyyy, HH:mm", Fr);
	}

	public static string Plural(int n, string singular, string plural) =>
		$"{n} {(n <= 1 ? singular : plural)}";

	public static string SafeFileName(string name)
	{
		var invalid = Path.GetInvalidFileNameChars();
		var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
		return string.IsNullOrWhiteSpace(cleaned) ? "document" : cleaned;
	}
}
