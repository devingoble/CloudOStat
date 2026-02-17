using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CloudOStat.App.Shared.Services;

namespace CloudOStat.App.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeviceController : ControllerBase
{
    private readonly ILogger<DeviceController> _logger;
    private readonly IConfiguration _configuration;
    
    private const int SasTokenExpiryMinutes = 60;
    private const int MinTelemetryInterval = 5;
    private const int MaxTelemetryInterval = 300;

    public DeviceController(ILogger<DeviceController> logger, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configuration);
        
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the current device status from device twin reported properties
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<DeviceStatus>> GetDeviceStatus(CancellationToken cancellationToken = default)
    {
        try
        {
            var iotHubName = _configuration["IoTHub:HubName"];
            var deviceId = _configuration["IoTHub:DeviceId"];
            var sharedAccessKey = _configuration["IoTHub:SharedAccessKey"];

            if (string.IsNullOrEmpty(iotHubName) || string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(sharedAccessKey))
            {
                _logger.LogError("IoT Hub configuration is missing");
                return StatusCode(StatusCodes.Status500InternalServerError, "Configuration error");
            }

            var iotHubUri = $"{iotHubName}.azure-devices.net";
            var uri = $"https://{iotHubUri}/twins/{deviceId}?api-version=2021-04-12";
            var sasToken = GenerateSasToken(iotHubUri, deviceId, sharedAccessKey);

            using var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("Authorization", $"SharedAccessSignature {sasToken}");

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get device status: {StatusCode}", response.StatusCode);
                return StatusCode((int)response.StatusCode, "Failed to get device status");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(content);
            var reported = doc.RootElement.GetProperty("properties").GetProperty("reported");

            var deviceStatus = new DeviceStatus
            {
                AirTemperature = TryGetDouble(reported, "air_temperature"),
                Meat1Temperature = TryGetDouble(reported, "meat1_temperature"),
                Meat2Temperature = TryGetDouble(reported, "meat2_temperature"),
                Status = TryGetString(reported, "device_status"),
                TelemetryIntervalSeconds = TryGetInt(reported, "telemetry_interval_seconds"),
                LastUpdate = TryGetDateTime(reported, "last_update"),
                IsConnected = true
            };

            return Ok(deviceStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device status");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error getting device status");
        }
    }

    /// <summary>
    /// Updates the desired telemetry reporting interval
    /// </summary>
    [HttpPost("twin/desired")]
    public async Task<ActionResult<DeviceControlResponse>> UpdateDesiredProperties(
        [FromBody] UpdateDesiredPropertiesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request?.TelemetryIntervalSeconds == null)
            {
                return BadRequest(new DeviceControlResponse
                {
                    Success = false,
                    Message = "telemetryIntervalSeconds is required"
                });
            }

            var intervalSeconds = request.TelemetryIntervalSeconds.Value;
            
            if (intervalSeconds < MinTelemetryInterval || intervalSeconds > MaxTelemetryInterval)
            {
                return BadRequest(new DeviceControlResponse
                {
                    Success = false,
                    Message = $"Invalid telemetry interval: {intervalSeconds}. Must be between {MinTelemetryInterval} and {MaxTelemetryInterval} seconds."
                });
            }

            var iotHubName = _configuration["IoTHub:HubName"];
            var deviceId = _configuration["IoTHub:DeviceId"];
            var sharedAccessKey = _configuration["IoTHub:SharedAccessKey"];

            if (string.IsNullOrEmpty(iotHubName) || string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(sharedAccessKey))
            {
                _logger.LogError("IoT Hub configuration is missing");
                return StatusCode(StatusCodes.Status500InternalServerError, "Configuration error");
            }

            var iotHubUri = $"{iotHubName}.azure-devices.net";
            var uri = $"https://{iotHubUri}/twins/{deviceId}/properties/desired?api-version=2021-04-12";
            var sasToken = GenerateSasToken(iotHubUri, deviceId, sharedAccessKey);

            var payload = new { telemetry_interval_seconds = intervalSeconds };
            var jsonPayload = JsonSerializer.Serialize(payload);

            using var httpClient = new HttpClient();
            var patchRequest = new HttpRequestMessage(HttpMethod.Patch, uri)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            patchRequest.Headers.Add("Authorization", $"SharedAccessSignature {sasToken}");

            using var response = await httpClient.SendAsync(patchRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully updated device telemetry interval to {Interval} seconds", intervalSeconds);
                
                return Ok(new DeviceControlResponse
                {
                    Success = true,
                    Message = $"Telemetry interval updated to {intervalSeconds} seconds"
                });
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to update device: {StatusCode} - {Error}", response.StatusCode, errorContent);
                
                return StatusCode((int)response.StatusCode, new DeviceControlResponse
                {
                    Success = false,
                    Message = $"Failed to update device: {response.StatusCode} - {errorContent}"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating desired properties");
            return StatusCode(StatusCodes.Status500InternalServerError, new DeviceControlResponse
            {
                Success = false,
                Message = "Error updating telemetry interval",
                Error = ex
            });
        }
    }

    private string GenerateSasToken(string iotHubUri, string deviceId, string sharedAccessKey)
    {
        var resourceUri = $"{iotHubUri}/devices/{deviceId}";
        var encodedResourceUri = Uri.EscapeDataString(resourceUri);
        
        var expiryTime = DateTimeOffset.UtcNow.AddMinutes(SasTokenExpiryMinutes).ToUnixTimeSeconds();
        var signatureString = $"{encodedResourceUri}\n{expiryTime}";

        var keyBytes = Convert.FromBase64String(sharedAccessKey);
        using var hmac = new HMACSHA256(keyBytes);
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
        var signature = Convert.ToBase64String(signatureBytes);

        return $"SharedAccessSignature sr={encodedResourceUri}&sig={Uri.EscapeDataString(signature)}&se={expiryTime}&skn=device";
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

public class UpdateDesiredPropertiesRequest
{
    public int? TelemetryIntervalSeconds { get; set; }
}
