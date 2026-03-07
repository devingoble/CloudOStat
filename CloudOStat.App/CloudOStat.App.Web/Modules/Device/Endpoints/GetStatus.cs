using CloudOStat.App.Web.Services;

namespace CloudOStat.App.Web.Modules.Device.Endpoints;

/// <summary>
/// Retrieves current device status from IoT Hub device twin reported properties.
/// </summary>
internal sealed class GetStatus
{
    private readonly IoTHubDeviceService _deviceService;

    public GetStatus(IoTHubDeviceService deviceService)
    {
        ArgumentNullException.ThrowIfNull(deviceService);
        _deviceService = deviceService;
    }

    /// <summary>
    /// Fetches the device twin and maps reported properties to a status response.
    /// </summary>
    public async Task<IResult> HandleAsync(CancellationToken cancellationToken)
    {
        var status = await _deviceService.GetDeviceStatusAsync(cancellationToken);

        return status is not null
            ? Results.Ok(status)
            : Results.Problem("Failed to get device status", statusCode: StatusCodes.Status502BadGateway);
    }
}
