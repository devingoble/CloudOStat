using CloudOStat.App.Shared.Services;
using CloudOStat.App.Web.Services;

namespace CloudOStat.App.Web.Modules.Device.Endpoints;

/// <summary>
/// Updates the desired telemetry reporting interval on the device via IoT Hub device twin.
/// </summary>
internal sealed class UpdateDesiredProperties
{
    private readonly IoTHubDeviceService _deviceService;

    public UpdateDesiredProperties(IoTHubDeviceService deviceService)
    {
        ArgumentNullException.ThrowIfNull(deviceService);
        _deviceService = deviceService;
    }

    /// <summary>
    /// Validates the requested interval and writes it to the device twin desired properties.
    /// </summary>
    public async Task<IResult> HandleAsync(
        UpdateDesiredPropertiesCommand command,
        CancellationToken cancellationToken)
    {
        if (command.TelemetryIntervalSeconds is null)
        {
            return Results.BadRequest(new DeviceControlResponse
            {
                Success = false,
                Message = "telemetryIntervalSeconds is required"
            });
        }

        var intervalSeconds = command.TelemetryIntervalSeconds.Value;

        if (!_deviceService.IsValidTelemetryInterval(intervalSeconds))
        {
            var (min, max) = _deviceService.GetTelemetryIntervalRange();
            return Results.BadRequest(new DeviceControlResponse
            {
                Success = false,
                Message = $"Invalid telemetry interval: {intervalSeconds}. Must be between {min} and {max} seconds."
            });
        }

        var result = await _deviceService.UpdateTelemetryIntervalAsync(intervalSeconds, cancellationToken);

        return result.Success
            ? Results.Ok(result)
            : Results.Problem(result.Message, statusCode: StatusCodes.Status502BadGateway);
    }

    /// <summary>
    /// Request body for updating device twin desired properties.
    /// </summary>
    public sealed record UpdateDesiredPropertiesCommand(int? TelemetryIntervalSeconds);
}
