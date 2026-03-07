using CloudOStat.App.Web.Modules.Device.Endpoints;

namespace CloudOStat.App.Web.Modules.Device;

/// <summary>
/// Maps device control API endpoints under <c>/api/device</c>.
/// </summary>
internal static class DeviceModule
{
    /// <summary>
    /// Registers device control routes with the application's endpoint routing.
    /// </summary>
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/device");

        group.MapGet("/status", async (GetStatus endpoint, CancellationToken ct) =>
            await endpoint.HandleAsync(ct))
            .WithName("GetDeviceStatus")
            .WithSummary("Gets the current device status from device twin reported properties");

        group.MapPost("/twin/desired", async (
            UpdateDesiredProperties.UpdateDesiredPropertiesCommand command,
            UpdateDesiredProperties endpoint,
            CancellationToken ct) =>
            await endpoint.HandleAsync(command, ct))
            .WithName("UpdateDesiredProperties")
            .WithSummary("Updates the desired telemetry reporting interval");

        return endpoints;
    }
}
