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
}
