using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CloudOStat.App.Shared.Services;
using Microsoft.Extensions.Configuration;

namespace CloudOStat.App.Services;

/// <summary>
/// MAUI implementation of IDeviceControlService
/// Communicates directly with Azure IoT Hub REST API to manage device twin desired properties
/// </summary>
public class DeviceControlService : IDeviceControlService
{
    private readonly HttpClient _httpClient;
    private readonly string _iotHubName;
    private readonly string _deviceId;
    private readonly string _sharedAccessKey;
    private readonly string _sharedAccessKeyName;
    private readonly string _iotHubUri;
    
    private const int MinTelemetryInterval = 5;
    private const int MaxTelemetryInterval = 300;
    private const int SasTokenExpiryMinutes = 60;
    private static readonly TimeSpan StaleDeviceThreshold = TimeSpan.FromMinutes(10);

    public DeviceControlService(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        
        // Load configuration from appsettings.json or environment variables
        _iotHubName = configuration["IoTHub:HubName"] ?? "usw-iot-cloudostat";
        _deviceId = configuration["IoTHub:DeviceId"] ?? "cloudostat-meadow";
        _sharedAccessKey = configuration["IoTHub:SharedAccessKey"] ?? string.Empty;
        _sharedAccessKeyName = configuration["IoTHub:SharedAccessKeyName"] ?? "iothubowner";
        _iotHubUri = $"{_iotHubName}.azure-devices.net";
        
        _httpClient = new HttpClient();
    }

    public (int MinSeconds, int MaxSeconds) GetTelemetryIntervalRange()
    {
        return (MinTelemetryInterval, MaxTelemetryInterval);
    }

    public bool IsValidTelemetryInterval(int intervalSeconds)
    {
        return intervalSeconds >= MinTelemetryInterval && intervalSeconds <= MaxTelemetryInterval;
    }

    public async Task<DeviceStatus?> GetDeviceStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = BuildDeviceTwinUri();
            var sasToken = GenerateSasToken();
            
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("Authorization", $"SharedAccessSignature {sasToken}");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(content);
            var reported = doc.RootElement.GetProperty("properties").GetProperty("reported");

            var lastUpdate = TryGetDateTime(reported, "last_update");
            var isConnected = lastUpdate.HasValue && (DateTime.UtcNow - lastUpdate.Value) < StaleDeviceThreshold;
            var statusRaw = TryGetString(reported, "device_status");

            return new DeviceStatus
            {
                AirTemperature = TryGetDouble(reported, "air_temperature"),
                Meat1Temperature = TryGetDouble(reported, "meat1_temperature"),
                Meat2Temperature = TryGetDouble(reported, "meat2_temperature"),
                Status = statusRaw,
                TelemetryIntervalSeconds = TryGetInt(reported, "telemetry_interval_seconds"),
                LastUpdate = lastUpdate,
                IsConnected = isConnected,
                StatusKind = DeviceStatus.ParseStatusKind(statusRaw, isConnected)
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting device status: {ex.Message}");
            return null;
        }
    }

    public async Task<DeviceControlResponse> UpdateTelemetryIntervalAsync(int intervalSeconds, CancellationToken cancellationToken = default)
    {
        if (!IsValidTelemetryInterval(intervalSeconds))
        {
            return new DeviceControlResponse
            {
                Success = false,
                Message = $"Invalid telemetry interval: {intervalSeconds}. Must be between {MinTelemetryInterval} and {MaxTelemetryInterval} seconds."
            };
        }

        try
        {
            var uri = BuildDeviceTwinDesiredUri();
            var sasToken = GenerateSasToken();
            
            var payload = new { telemetry_interval_seconds = intervalSeconds };
            var jsonPayload = JsonSerializer.Serialize(payload);

            var request = new HttpRequestMessage(HttpMethod.Patch, uri)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"SharedAccessSignature {sasToken}");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return new DeviceControlResponse
                {
                    Success = true,
                    Message = $"Telemetry interval updated to {intervalSeconds} seconds"
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return new DeviceControlResponse
                {
                    Success = false,
                    Message = $"Failed to update device: {response.StatusCode} - {errorContent}"
                };
            }
        }
        catch (Exception ex)
        {
            return new DeviceControlResponse
            {
                Success = false,
                Message = $"Error updating telemetry interval: {ex.Message}",
                Error = ex
            };
        }
    }

    private string BuildDeviceTwinUri()
    {
        return $"https://{_iotHubUri}/twins/{_deviceId}?api-version=2021-04-12";
    }

    private string BuildDeviceTwinDesiredUri()
    {
        return $"https://{_iotHubUri}/twins/{_deviceId}/properties/desired?api-version=2021-04-12";
    }

    private string GenerateSasToken()
    {
        var encodedResourceUri = Uri.EscapeDataString(_iotHubUri);

        var expiryTime = DateTimeOffset.UtcNow.AddMinutes(SasTokenExpiryMinutes).ToUnixTimeSeconds();
        var signatureString = $"{encodedResourceUri}\n{expiryTime}";

        var keyBytes = Convert.FromBase64String(_sharedAccessKey);
        using var hmac = new HMACSHA256(keyBytes);
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
        var signature = Convert.ToBase64String(signatureBytes);

        return $"sr={encodedResourceUri}&sig={Uri.EscapeDataString(signature)}&se={expiryTime}&skn={_sharedAccessKeyName}";
    }

    private static double? TryGetDouble(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.TryGetDouble(out var value))
        {
            return value;
        }
        return null;
    }

    private static int? TryGetInt(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.TryGetInt32(out var value))
        {
            return value;
        }
        return null;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private static DateTime? TryGetDateTime(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.TryGetDateTime(out var value))
            {
                return value;
            }
            else if (prop.ValueKind == JsonValueKind.String && DateTime.TryParse(prop.GetString(), out var parsed))
            {
                return parsed;
            }
        }
        return null;
    }
}
