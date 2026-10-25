using Artemix.Models;
using Microsoft.Maui.Storage;
using SkiaSharp;

namespace Artemix.Services;

/// <summary>Export d'un document en PDF : un PDF est copié tel quel, une image est convertie en PDF (SkiaSharp).</summary>
public class PdfService
{
	public static string ShareDir
	{
		get
		{
			var dir = Path.Combine(FileSystem.CacheDirectory, "share");
			Directory.CreateDirectory(dir);
			return dir;
		}
	}

	public async Task<string> ExportToPdfAsync(DocumentItem doc)
	{
		var target = Path.Combine(ShareDir, Format.SafeFileName(doc.Name) + ".pdf");

		if (doc.IsPdf)
		{
			File.Copy(doc.FullPath, target, true);
			return target;
		}

		if (!doc.IsImage)
			throw new NotSupportedException("Ce type de fichier ne peut pas être converti en PDF.");

		await Task.Run(() => ImageToPdf(doc.FullPath, target));
		return target;
	}

	/// <summary>Copie le fichier original dans le cache (nom lisible) pour l'ouvrir ou le partager.</summary>
	public string CopyToShareDir(DocumentItem doc)
	{
		var target = Path.Combine(ShareDir, Format.SafeFileName(doc.Name) + doc.Extension);
		File.Copy(doc.FullPath, target, true);
		return target;
	}

	private static void ImageToPdf(string source, string destination)
	{
		using var bmp = LoadOriented(source);
		const float pageWidth = 595f; // largeur A4 en points
		var pageHeight = bmp.Height * (pageWidth / bmp.Width);

		using var pdf = SKDocument.CreatePdf(destination);
		using (var canvas = pdf.BeginPage(pageWidth, pageHeight))
		{
			canvas.DrawBitmap(bmp, new SKRect(0, 0, pageWidth, pageHeight));
		}
		pdf.EndPage();
		pdf.Close();
	}

	private static SKBitmap LoadOriented(string path)
	{
		using var stream = File.OpenRead(path);
		using var codec = SKCodec.Create(stream) ?? throw new InvalidDataException("Image illisible.");
		var origin = codec.EncodedOrigin;
		var bmp = SKBitmap.Decode(codec) ?? throw new InvalidDataException("Image illisible.");

		SKBitmap? rotated = origin switch
		{
			SKEncodedOrigin.RightTop => Rotate(bmp, 90),
			SKEncodedOrigin.BottomRight => Rotate(bmp, 180),
			SKEncodedOrigin.LeftBottom => Rotate(bmp, 270),
			_ => null,
		};

		if (rotated == null) return bmp;
		bmp.Dispose();
		return rotated;
	}

	private static SKBitmap Rotate(SKBitmap src, int degrees)
	{
		var swap = degrees != 180;
		var dst = new SKBitmap(swap ? src.Height : src.Width, swap ? src.Width : src.Height);
		using var canvas = new SKCanvas(dst);
		canvas.Translate(dst.Width / 2f, dst.Height / 2f);
		canvas.RotateDegrees(degrees);
		canvas.Translate(-src.Width / 2f, -src.Height / 2f);
		canvas.DrawBitmap(src, 0, 0);
		return dst;
	}
}
