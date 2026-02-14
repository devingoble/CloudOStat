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

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
