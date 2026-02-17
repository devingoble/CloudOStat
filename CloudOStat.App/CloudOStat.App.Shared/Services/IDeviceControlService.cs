namespace CloudOStat.App.Shared.Services;

/// <summary>
/// Device status information retrieved from device twin reported properties
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
