using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CloudOStat.App.Shared.Services;
using CloudOStat.App.Services;
using MudBlazor.Services;

namespace CloudOStat.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Add configuration from appsettings.json embedded in app resources
        var configBuilder = new ConfigurationBuilder();
        AddConfigurationFiles(configBuilder);
        var configuration = configBuilder.Build();
        builder.Services.AddSingleton<IConfiguration>(configuration);

        builder.Services.AddMudServices();
        builder.Services.AddSingleton<NavigationService>();

        // Add device-specific services used by the CloudOStat.App.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();
        builder.Services.AddSingleton<IDeviceControlService, DeviceControlService>();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        
        // Filter out known MudBlazor JSInterop warnings in MAUI
        builder.Logging.AddFilter("Microsoft.AspNetCore.Components.RenderTree.Renderer", (level) =>
        {
            // Allow errors but suppress the mudElementRef ones in the error handler
            return level >= LogLevel.Warning;
        });
#else
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
#endif

        // Configure logging to capture errors but reduce noise
        builder.Logging.AddFilter("Microsoft.AspNetCore.Components.RenderTree", LogLevel.Warning);

        return builder.Build();
    }

    /// <summary>
    /// Add configuration files from the app resources.
    /// Loads appsettings.json and optionally environment-specific overrides.
    /// </summary>
    private static void AddConfigurationFiles(IConfigurationBuilder configBuilder)
    {
        try
        {
            // Load default appsettings.json from app resources
            var appsettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(appsettingsPath))
            {
                configBuilder.AddJsonFile(appsettingsPath, optional: true, reloadOnChange: false);
            }
            else
            {
                // Fallback: try to load from assembly resources
                var assembly = typeof(MauiProgram).Assembly;
                using var stream = assembly.GetManifestResourceStream("CloudOStat.App.appsettings.json");
                if (stream != null)
                {
                    configBuilder.AddJsonStream(stream);
                }
            }

            // Load environment-specific settings if they exist
#if DEBUG
            var debugPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Debug.json");
            if (File.Exists(debugPath))
            {
                configBuilder.AddJsonFile(debugPath, optional: true, reloadOnChange: false);
            }
#else
            var releasePath = Path.Combine(AppContext.BaseDirectory, "appsettings.Release.json");
            if (File.Exists(releasePath))
            {
                configBuilder.AddJsonFile(releasePath, optional: true, reloadOnChange: false);
            }
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
        }
    }
}
