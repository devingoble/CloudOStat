using System.Net.Http.Json;
using CloudOStat.App.Shared.Services;

namespace CloudOStat.App.Web.Client.Services;

/// <summary>
/// WebAssembly implementation of IDeviceControlService
/// Makes HTTP calls to backend API instead of directly accessing Azure IoT Hub
/// (For security: browsers cannot make direct HTTPS calls to Azure IoT Hub with SAS tokens)
/// </summary>
public class DeviceControlService : IDeviceControlService
{
    private readonly HttpClient _httpClient;
    
    private const int MinTelemetryInterval = 5;
    private const int MaxTelemetryInterval = 300;

    public DeviceControlService(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
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
            using var response = await _httpClient.GetAsync("api/device/status", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DeviceStatus>(cancellationToken: cancellationToken);
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
            var payload = new { telemetryIntervalSeconds = intervalSeconds };
            
            using var response = await _httpClient.PostAsJsonAsync("api/device/twin/desired", payload, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DeviceControlResponse>(cancellationToken: cancellationToken) 
                    ?? new DeviceControlResponse
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
}
