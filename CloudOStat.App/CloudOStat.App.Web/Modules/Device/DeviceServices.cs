using CloudOStat.App.Web.Modules.Device.Endpoints;
using CloudOStat.App.Web.Services;

namespace CloudOStat.App.Web.Modules.Device;

/// <summary>
/// Registers device module services with the dependency injection container.
/// </summary>
internal static class DeviceServices
{
    /// <summary>
    /// Adds IoT Hub device service and endpoint handlers to the service collection.
    /// </summary>
    public static IServiceCollection RegisterDeviceServices(this IServiceCollection services)
    {
        // IoT Hub communication service
        services.AddHttpClient();
        services.AddSingleton<IoTHubDeviceService>();

        // Endpoint handlers — one per operation
        services.AddScoped<GetStatus>();
        services.AddScoped<UpdateDesiredProperties>();

        return services;
    }
}
