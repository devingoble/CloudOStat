using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CloudOStat.App.Shared.Services;

namespace CloudOStat.App.Web.Services;

/// <summary>
/// Server-side implementation of <see cref="IDeviceControlService"/> that communicates
/// directly with Azure IoT Hub REST API to read device twin reported properties
/// and write desired properties. Used by the web server's API endpoints.
/// </summary>
internal sealed class IoTHubDeviceService : IDeviceControlService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IoTHubDeviceService> _logger;

    private const int SasTokenExpiryMinutes = 60;
    private const int MinTelemetryInterval = 5;
    private const int MaxTelemetryInterval = 300;
    private const string IoTHubApiVersion = "2021-04-12";

    public IoTHubDeviceService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<IoTHubDeviceService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public (int MinSeconds, int MaxSeconds) GetTelemetryIntervalRange() =>
        (MinTelemetryInterval, MaxTelemetryInterval);

    public bool IsValidTelemetryInterval(int intervalSeconds) =>
        intervalSeconds >= MinTelemetryInterval && intervalSeconds <= MaxTelemetryInterval;

    public async Task<DeviceStatus?> GetDeviceStatusAsync(CancellationToken cancellationToken = default)
    {
        var (iotHubUri, deviceId, sharedAccessKey) = GetIoTHubConfig();

        if (string.IsNullOrEmpty(iotHubUri) || string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(sharedAccessKey))
        {
            _logger.LogError("IoT Hub configuration is missing");
            return null;
        }

        var uri = $"https://{iotHubUri}/twins/{deviceId}?api-version={IoTHubApiVersion}";
        var sasToken = GenerateSasToken(iotHubUri, sharedAccessKey);

        using var httpClient = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("Authorization", $"SharedAccessSignature {sasToken}");

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to get device status: {StatusCode}", response.StatusCode);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var reported = doc.RootElement.GetProperty("properties").GetProperty("reported");

        return new DeviceStatus
        {
            AirTemperature = TryGetDouble(reported, "air_temperature"),
            Meat1Temperature = TryGetDouble(reported, "meat1_temperature"),
            Meat2Temperature = TryGetDouble(reported, "meat2_temperature"),
            Status = TryGetString(reported, "device_status"),
            TelemetryIntervalSeconds = TryGetInt(reported, "telemetry_interval_seconds"),
            LastUpdate = TryGetDateTime(reported, "last_update"),
            IsConnected = true
        };
    }

    public async Task<DeviceControlResponse> UpdateTelemetryIntervalAsync(
        int intervalSeconds,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidTelemetryInterval(intervalSeconds))
        {
            return new DeviceControlResponse
            {
                Success = false,
                Message = $"Invalid telemetry interval: {intervalSeconds}. Must be between {MinTelemetryInterval} and {MaxTelemetryInterval} seconds."
            };
        }

        var (iotHubUri, deviceId, sharedAccessKey) = GetIoTHubConfig();

        if (string.IsNullOrEmpty(iotHubUri) || string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(sharedAccessKey))
        {
            _logger.LogError("IoT Hub configuration is missing");
            return new DeviceControlResponse
            {
                Success = false,
                Message = "IoT Hub configuration is missing"
            };
        }

        var uri = $"https://{iotHubUri}/twins/{deviceId}/properties/desired?api-version={IoTHubApiVersion}";
        var sasToken = GenerateSasToken(iotHubUri, sharedAccessKey);

        var payload = new { telemetry_interval_seconds = intervalSeconds };
        var jsonPayload = JsonSerializer.Serialize(payload);

        using var httpClient = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Patch, uri)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"SharedAccessSignature {sasToken}");

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully updated device telemetry interval to {Interval} seconds", intervalSeconds);

            return new DeviceControlResponse
            {
                Success = true,
                Message = $"Telemetry interval updated to {intervalSeconds} seconds"
            };
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Failed to update device: {StatusCode} - {Error}", response.StatusCode, errorContent);

        return new DeviceControlResponse
        {
            Success = false,
            Message = $"Failed to update device: {response.StatusCode} - {errorContent}"
        };
    }

    private (string? IotHubUri, string? DeviceId, string? SharedAccessKey) GetIoTHubConfig()
    {
        var hubName = _configuration["IoTHub:HubName"];
        var deviceId = _configuration["IoTHub:DeviceId"];
        var sharedAccessKey = _configuration["IoTHub:SharedAccessKey"];

        var iotHubUri = string.IsNullOrEmpty(hubName) ? null : $"{hubName}.azure-devices.net";

        return (iotHubUri, deviceId, sharedAccessKey);
    }

    private string GenerateSasToken(string iotHubUri, string sharedAccessKey)
    {
        var encodedResourceUri = Uri.EscapeDataString(iotHubUri);

        var expiryTime = DateTimeOffset.UtcNow.AddMinutes(SasTokenExpiryMinutes).ToUnixTimeSeconds();
        var signatureString = $"{encodedResourceUri}\n{expiryTime}";

        var keyBytes = Convert.FromBase64String(sharedAccessKey);
        using var hmac = new HMACSHA256(keyBytes);
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
        var signature = Convert.ToBase64String(signatureBytes);

        var keyName = _configuration["IoTHub:SharedAccessKeyName"] ?? "iothubowner";
        return $"sr={encodedResourceUri}&sig={Uri.EscapeDataString(signature)}&se={expiryTime}&skn={keyName}";
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
