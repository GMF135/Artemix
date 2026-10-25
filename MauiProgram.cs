using Artemix.Pages;
using Artemix.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Artemix;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>();

		// Services
		builder.Services.AddSingleton<SecurityService>();
		builder.Services.AddSingleton<DocumentService>();
		builder.Services.AddSingleton<BackupService>();
		builder.Services.AddSingleton<PdfService>();
#if ANDROID
		builder.Services.AddSingleton<IBiometricService, AndroidBiometricService>();
#else
		builder.Services.AddSingleton<IBiometricService, NoBiometricService>();
#endif

		// Pages
		builder.Services.AddTransient<AppShell>();
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<SearchPage>();
		builder.Services.AddTransient<BackupPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<CategoryPage>();
		builder.Services.AddTransient<ImportPage>();
		builder.Services.AddTransient<DetailPage>();

		// Champs de saisie sans soulignement (Android) : le contour est porté par la carte autour
		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("ArtemixNoUnderline", (handler, view) =>
		{
#if ANDROID
			handler.PlatformView.BackgroundTintList =
				Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
		});

		return builder.Build();
	}
}
