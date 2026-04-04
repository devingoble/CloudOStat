namespace CloudOStat.App.Shared.Services;

/// <summary>
/// Typed representation of the device's operational state reported via device twin.
/// </summary>
public enum DeviceOperationalStatus
{
    /// <summary>Status is absent or could not be parsed.</summary>
    Unknown,
    /// <summary>Heater relay is active; temperature is below target.</summary>
    Heating,
    /// <summary>Temperature is at the target setpoint; relay is off.</summary>
    OnTemp,
    /// <summary>Temperature is above the target setpoint; relay is off.</summary>
    Over,
    /// <summary>Device has not reported within the staleness threshold.</summary>
    Offline,
    /// <summary>Device reported a sensor or communication error.</summary>
    Error
}

/// <summary>
/// Device status information retrieved from device twin reported properties.
/// </summary>
public class DeviceStatus
{
    public double? AirTemperature { get; set; }
    public double? Meat1Temperature { get; set; }
    public double? Meat2Temperature { get; set; }
    public string? Status { get; set; }
    public int? TelemetryIntervalSeconds { get; set; }
    public DateTime? LastUpdate { get; set; }
    public bool IsConnected { get; set; }

    /// <summary>
    /// Typed operational status derived from the <see cref="Status"/> string and
    /// <see cref="IsConnected"/> flag. Use this for UI color-coding.
    /// </summary>
    public DeviceOperationalStatus StatusKind { get; set; }

    /// <summary>
    /// Parses a raw device-twin status string into a <see cref="DeviceOperationalStatus"/> value.
    /// </summary>
    /// <param name="raw">Value of the <c>device_status</c> reported property.</param>
    /// <param name="isConnected">Whether the device is considered online.</param>
    public static DeviceOperationalStatus ParseStatusKind(string? raw, bool isConnected)
    {
        if (!isConnected)
            return DeviceOperationalStatus.Offline;

        return raw switch
        {
            "Heating" => DeviceOperationalStatus.Heating,
            "On Temp" => DeviceOperationalStatus.OnTemp,
            "Over" => DeviceOperationalStatus.Over,
            null or "" => DeviceOperationalStatus.Unknown,
            _ => DeviceOperationalStatus.Error  // error strings like "air:..." fall here
        };
    }
}

/// <summary>
/// Response from device control operations
/// </summary>
public class DeviceControlResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public DeviceStatus? UpdatedStatus { get; set; }
    public Exception? Error { get; set; }
}

/// <summary>
/// Service for controlling the Meadow device via Azure IoT Hub device twin
/// Abstracts device control operations for use across MAUI and Blazor platforms
/// </summary>
public interface IDeviceControlService
{
    /// <summary>
    /// Gets the current device status from device twin reported properties
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Device status information or null if unavailable</returns>
    Task<DeviceStatus?> GetDeviceStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the desired telemetry reporting interval on the device
    /// </summary>
    /// <param name="intervalSeconds">New interval in seconds (must be 5-300)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating success or failure</returns>
    Task<DeviceControlResponse> UpdateTelemetryIntervalAsync(int intervalSeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the allowed range for telemetry interval
    /// </summary>
    /// <returns>Tuple of (min seconds, max seconds)</returns>
    (int MinSeconds, int MaxSeconds) GetTelemetryIntervalRange();

    /// <summary>
    /// Validates if a telemetry interval value is within the allowed range
    /// </summary>
    /// <param name="intervalSeconds">Interval to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValidTelemetryInterval(int intervalSeconds);
}
