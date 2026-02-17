using System;
using System.Text.Json.Serialization;

namespace CloudOStat.LocalHardware;

/// <summary>
/// Azure IoT Hub device twin properties for CloudOStat sensor readings and configuration
/// </summary>
public class DeviceTwinProperties
{
    /// <summary>
    /// Reported properties contain the current state of the device as reported to Azure
    /// </summary>
    public class Reported
    {
        [JsonPropertyName("air_temperature")]
        public double? AirTemperature { get; set; }

        [JsonPropertyName("meat1_temperature")]
        public double? Meat1Temperature { get; set; }

        [JsonPropertyName("meat2_temperature")]
        public double? Meat2Temperature { get; set; }

        [JsonPropertyName("device_status")]
        public string? DeviceStatus { get; set; }

        [JsonPropertyName("telemetry_interval_seconds")]
        public int? TelemetryIntervalSeconds { get; set; }

        [JsonPropertyName("last_update")]
        public DateTime? LastUpdate { get; set; }
    }

    /// <summary>
    /// Desired properties contain the configuration that the device should apply
    /// </summary>
    public class Desired
    {
        [JsonPropertyName("telemetry_interval_seconds")]
        public int? TelemetryIntervalSeconds { get; set; }

        [JsonPropertyName("$version")]
        public int Version { get; set; }
    }
}
