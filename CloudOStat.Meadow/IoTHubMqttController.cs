using Meadow;
using Meadow.Units;

using MQTTnet;
using MQTTnet.Client;

using System;
using System.Globalization;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using CloudOStat.LocalHardware;

namespace CloudOStat.Controllers;

internal class IoTHubMqttController : IIoTHubController, IDisposable
{
    readonly string IOT_HUB_DEVICE_ID = Secrets.DEVICE_ID;
    readonly string IOT_HUB_CONNECTION_STRING = Secrets.CONNECTION_STRING;

    const int SasTokenExpiryMinutes = 60;
    static readonly TimeSpan SasTokenRefreshBuffer = TimeSpan.FromMinutes(15);
    static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(20);
    static readonly TimeSpan MaxReconnectWait = TimeSpan.FromSeconds(60);
    static readonly TimeSpan PublishTimeout = TimeSpan.FromSeconds(30);
    const int MaxD2CMessageBytes = 262_144; // Azure IoT Hub D2C limit: 256 KB

    IMqttClient mqttClient;
    readonly SemaphoreSlim _connectionLock = new(1, 1);
    string _iotHubUri;
    string _username;
    DateTimeOffset _sasTokenExpiryUtc;
    string _hostName;
    string _sharedAccessKey;
    string _deviceId;
    DateTime _lastConnectAttempt = DateTime.MinValue;
    int _connectFailureCount = 0;

    public bool isAuthenticated { get; private set; }

    // Device Twin event
    public event EventHandler<DeviceTwinDesiredPropertiesEventArgs>? DesiredPropertiesReceived;

    public IoTHubMqttController() { }

    private void EnsureConnectionSettingsInitialized()
    {
        if (!string.IsNullOrWhiteSpace(_hostName) && !string.IsNullOrWhiteSpace(_sharedAccessKey) && !string.IsNullOrWhiteSpace(_deviceId))
        {
            return;
        }

        var parts = IOT_HUB_CONNECTION_STRING.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        string hostName = null;
        string sharedAccessKey = null;
        string deviceId = null;

        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            if (trimmedPart.StartsWith("HostName="))
            {
                hostName = trimmedPart.Substring("HostName=".Length);
            }
            else if (trimmedPart.StartsWith("SharedAccessKey="))
            {
                sharedAccessKey = trimmedPart.Substring("SharedAccessKey=".Length);
            }
            else if (trimmedPart.StartsWith("DeviceId="))
            {
                deviceId = trimmedPart.Substring("DeviceId=".Length);
            }
        }

        if (string.IsNullOrEmpty(hostName))
        {
            throw new InvalidOperationException("Connection string is missing HostName");
        }

        if (string.IsNullOrEmpty(sharedAccessKey))
        {
            throw new InvalidOperationException("Connection string is missing SharedAccessKey");
        }

        _hostName = hostName;
        _sharedAccessKey = sharedAccessKey;
        _deviceId = !string.IsNullOrWhiteSpace(deviceId) ? deviceId : IOT_HUB_DEVICE_ID;

        _iotHubUri = _hostName;
        _username = $"{_hostName}/{_deviceId}/?api-version=2021-04-12";
    }

    /// <summary>
    /// Generates a SAS token from the IoT Hub connection string
    /// </summary>
    private string GenerateSasToken(out DateTimeOffset expiryUtc, int expiryMinutes = SasTokenExpiryMinutes)
    {
        try
        {
            EnsureConnectionSettingsInitialized();

            Resolver.Log.Info($"DEBUG: Parsed HostName: {_hostName}");
            Resolver.Log.Info($"DEBUG: Using DeviceId: {_deviceId}");

            // Create resource URI for the device
            var resourceUri = $"{_hostName}/devices/{_deviceId}";

            // URL-encode the resource URI BEFORE signing (critical for authentication)
            var encodedResourceUri = Uri.EscapeDataString(resourceUri);
            Resolver.Log.Info($"DEBUG: Encoded Resource URI: {encodedResourceUri}");

            // Calculate expiry (Unix timestamp)
            expiryUtc = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);
            var expiryTime = expiryUtc.ToUnixTimeSeconds();
            Resolver.Log.Info($"DEBUG: Expiry time: {expiryTime}");

            // Create the signature string with URL-encoded resource URI
            var signatureString = $"{encodedResourceUri}\n{expiryTime}";

            // Sign with HMAC-SHA256
            var keyBytes = Convert.FromBase64String(_sharedAccessKey);
            using (var hmac = new HMACSHA256(keyBytes))
            {
                var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
                var signature = Convert.ToBase64String(signatureBytes);
                Resolver.Log.Info("DEBUG: Signature generated successfully");

                // Build the SAS token for device authentication
                // Note: encodedResourceUri is already URL-encoded, signature needs encoding
                var sasToken = $"SharedAccessSignature sr={encodedResourceUri}&sig={Uri.EscapeDataString(signature)}&se={expiryTime}";
                Resolver.Log.Info("DEBUG: SAS token generated successfully");
                return sasToken;
            }
        }
        catch (Exception ex)
        {
            Resolver.Log.Info($"Failed to generate SAS token: {ex.Message}");
            Resolver.Log.Info($"Exception type: {ex.GetType().Name}");
            throw;
        }
    }

    private MqttClientOptions BuildMqttOptions()
    {
        EnsureConnectionSettingsInitialized();

        Resolver.Log.Info("Generating SAS token...");
        var sasToken = GenerateSasToken(out var expiryUtc);
        _sasTokenExpiryUtc = expiryUtc;

        Resolver.Log.Info("Creating MQTT options ...");
        return new MqttClientOptionsBuilder()
            .WithClientId(_deviceId)
            .WithTcpServer(_iotHubUri, 8883)
            .WithCredentials(_username, sasToken)
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
            .WithTlsOptions(o =>
            {
                o.UseTls();
                o.WithSslProtocols(SslProtocols.Tls12);

                // Allow connection without certificate validation
                // This is necessary for embedded devices like Meadow that may not have
                // the root CA certificates installed
                o.WithCertificateValidationHandler(context =>
                {
                    // Log certificate info for debugging
                    if (context.Certificate != null)
                    {
                        Resolver.Log.Info($"Certificate Subject: {context.Certificate.Subject}");
                        Resolver.Log.Info($"Certificate Issuer: {context.Certificate.Issuer}");
                    }

                    // Accept the certificate
                    return true;
                });
            })
            .Build();
    }

    private bool IsSasTokenExpiringSoon()
    {
        return _sasTokenExpiryUtc != default && _sasTokenExpiryUtc <= DateTimeOffset.UtcNow.Add(SasTokenRefreshBuffer);
    }

    private async Task<bool> TryConnectAsync()
    {
        try
        {
            Resolver.Log.Info($"Azure Connecting... (attempt {_connectFailureCount + 1})");
            using var connectCts = new CancellationTokenSource(ConnectTimeout);
            await mqttClient.ConnectAsync(BuildMqttOptions(), connectCts.Token);

            isAuthenticated = true;
            _connectFailureCount = 0;
            Resolver.Log.Info("Successfully connected to Azure IoT Hub");

            // Subscribe to Device Twin topics after successful connection
            await SubscribeToDeviceTwinTopicsAsync();

            return true;
        }
        catch (OperationCanceledException)
        {
            Resolver.Log.Info($"Azure connection timed out (>{ConnectTimeout.TotalSeconds}s)");
            return false;
        }
        catch (Exception ex)
        {
            Resolver.Log.Info($"Azure connection error: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        // Fast path: connected and token is still valid
        if (mqttClient != null && mqttClient.IsConnected && !IsSasTokenExpiringSoon())
        {
            return true;
        }

        await _connectionLock.WaitAsync();
        try
        {
            // Re-check under lock: another caller may have already reconnected
            if (mqttClient != null && mqttClient.IsConnected && !IsSasTokenExpiringSoon())
            {
                return true;
            }

            // If connected but SAS token is expiring, tear down so we can reconnect with a fresh token
            if (mqttClient != null && mqttClient.IsConnected && IsSasTokenExpiringSoon())
            {
                Resolver.Log.Info("SAS token is expiring soon, initiating reconnection...");
                await DisconnectAndDisposeClientAsync();
            }

            // Rate limit reconnection attempts with exponential backoff
            // Skip backoff when reconnecting for SAS token refresh (_connectFailureCount == 0)
            if (_connectFailureCount > 0)
            {
                var timeSinceLastAttempt = DateTime.UtcNow - _lastConnectAttempt;
                var minWait = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, _connectFailureCount), MaxReconnectWait.TotalSeconds));

                if (timeSinceLastAttempt < minWait)
                {
                    Resolver.Log.Info($"Reconnection backoff active, waiting {(minWait - timeSinceLastAttempt).TotalSeconds:F1}s more...");
                    return false;
                }
            }

            if (mqttClient == null)
            {
                Resolver.Log.Info("Creating MQTT client...");
                var factory = new MqttFactory();
                mqttClient = factory.CreateMqttClient();
                mqttClient.DisconnectedAsync += args =>
                {
                    isAuthenticated = false;
                    Resolver.Log.Info($"MQTT disconnected: {args.ReasonString}");
                    return Task.CompletedTask;
                };
                mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
            }

            EnsureConnectionSettingsInitialized();

            _lastConnectAttempt = DateTime.UtcNow;
            if (await TryConnectAsync())
            {
                return true;
            }

            _connectFailureCount++;
            return false;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task DisconnectAndDisposeClientAsync()
    {
        // Fire-and-forget disconnect attempt (don't await—just let it happen in background)
        // Graceful disconnect on Meadow can hang, so we prioritize cleanup over politeness
        if (mqttClient != null && mqttClient.IsConnected)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await mqttClient.DisconnectAsync(new MqttClientDisconnectOptions(), cts.Token);
                    Resolver.Log.Info("Background disconnect completed");
                }
                catch
                {
                    // Silently ignore—we're disposing anyway
                }
            });
        }

        try
        {
            mqttClient?.Dispose();
        }
        catch (Exception ex)
        {
            Resolver.Log.Info($"Error disposing MQTT client: {ex.Message}");
        }

        mqttClient = null;
        isAuthenticated = false;
        _connectFailureCount = 0;
        Resolver.Log.Info("MQTT client cleaned up, ready for reconnection");
    }

    public async Task<bool> Initialize()
    {
        EnsureConnectionSettingsInitialized();

        return await EnsureConnectedAsync();
    }

    public async Task SendEnvironmentalReading(Temperature meatOne, Temperature meatTwo, Temperature air)
    {
        if (!await EnsureConnectedAsync())
        {
            throw new InvalidOperationException("IoT Hub connection is not available.");
        }

        Resolver.Log.Info("Create payload");

        string messagePayload =
            $"{{" +
            $"\"meat_one\":{meatOne.Fahrenheit.ToString(CultureInfo.InvariantCulture)}," +
            $"\"meat_two\":{meatTwo.Fahrenheit.ToString(CultureInfo.InvariantCulture)}," +
            $"\"air\":{air.Fahrenheit.ToString(CultureInfo.InvariantCulture)}" +
            $"}}";

        Resolver.Log.Info("Create message");
        Resolver.Log.Info(messagePayload);
        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"devices/{_deviceId}/messages/events/")
            .WithPayload(messagePayload)
            .Build();

        using var publishCts = new CancellationTokenSource(PublishTimeout);
        await mqttClient.PublishAsync(mqttMessage, publishCts.Token);

        Resolver.Log.Info($"*** MQTT - DATA SENT - Meat One - {meatOne.Fahrenheit}, Meat Two - {meatTwo.Fahrenheit}, Air - {air.Fahrenheit} ***");
    }

    public async Task SendBatchEnvironmentalReadings(List<TemperatureReading> readings)
    {
        if (!await EnsureConnectedAsync())
        {
            throw new InvalidOperationException("IoT Hub connection is not available.");
        }

        Resolver.Log.Info($"Creating batch payload with {readings.Count} readings");

        // Build JSON array of readings
        var readingsJson = new StringBuilder();
        readingsJson.Append("{\"readings\":[");

        for (int i = 0; i < readings.Count; i++)
        {
            var reading = readings[i];
            if (i > 0)
            {
                readingsJson.Append(",");
            }

            readingsJson.Append('{');
            readingsJson.Append($"\"timestamp\":\"{reading.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}\",");
            readingsJson.Append(string.Format(CultureInfo.InvariantCulture, "\"air\":{0},", reading.AirTemperature));
            readingsJson.Append(string.Format(CultureInfo.InvariantCulture, "\"meat_one\":{0},", reading.Meat1Temperature));
            readingsJson.Append(string.Format(CultureInfo.InvariantCulture, "\"meat_two\":{0},", reading.Meat2Temperature));
            readingsJson.Append($"\"status\":\"{reading.Status}\"");
            readingsJson.Append('}');
        }

        readingsJson.Append("]}");

        string messagePayload = readingsJson.ToString();
        Resolver.Log.Info("Create batch message");
        Resolver.Log.Info($"Batch payload size: {messagePayload.Length} bytes");

        if (Encoding.UTF8.GetByteCount(messagePayload) > MaxD2CMessageBytes)
        {
            throw new InvalidOperationException(
                $"Batch payload exceeds Azure IoT Hub D2C limit of {MaxD2CMessageBytes} bytes. Reduce batch size.");
        }

        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"devices/{_deviceId}/messages/events/")
            .WithPayload(messagePayload)
            .Build();

        using var publishCts = new CancellationTokenSource(PublishTimeout);
        await mqttClient.PublishAsync(mqttMessage, publishCts.Token);

        Resolver.Log.Info($"*** MQTT - BATCH SENT - {readings.Count} readings ***");
    }

    /// <summary>
    /// Subscribe to Device Twin MQTT topics for desired property updates
    /// </summary>
    private async Task SubscribeToDeviceTwinTopicsAsync()
    {
        try
        {
            Resolver.Log.Info("Subscribing to Device Twin topics...");

            // Subscribe to desired properties updates (PATCH)
            var desiredPatchTopic = "$iothub/twin/PATCH/properties/desired/#";
            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(desiredPatchTopic)
                .Build());
            Resolver.Log.Info($"Subscribed to {desiredPatchTopic}");

            // Subscribe to twin response topic (for GET/PATCH responses)
            var responseTopic = "$iothub/twin/res/#";
            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(responseTopic)
                .Build());
            Resolver.Log.Info($"Subscribed to {responseTopic}");

            // Request the full device twin state on connect
            await RequestDeviceTwinAsync();
        }
        catch (Exception ex)
        {
            Resolver.Log.Info($"Error subscribing to Device Twin topics: {ex.Message}");
        }
    }

    /// <summary>
    /// Request the full device twin state from Azure IoT Hub
    /// </summary>
    private async Task RequestDeviceTwinAsync()
    {
        try
        {
            var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var getTwinTopic = $"$iothub/twin/GET/?$rid={requestId}";
            
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(getTwinTopic)
                .Build();

            await mqttClient.PublishAsync(message);
            Resolver.Log.Info($"Requested device twin (rid={requestId})");
        }
        catch (Exception ex)
        {
            Resolver.Log.Info($"Error requesting device twin: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle incoming MQTT messages (Device Twin updates)
    /// </summary>
    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var topic = args.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

            Resolver.Log.Info($"Received message on topic: {topic}");

            // Handle desired properties PATCH
            if (topic.StartsWith("$iothub/twin/PATCH/properties/desired/"))
            {
                HandleDesiredPropertiesPatch(payload);
            }
            // Handle twin GET/PATCH response
            else if (topic.StartsWith("$iothub/twin/res/"))
            {
                HandleTwinResponse(topic, payload);
            }
        }
        catch (Exception ex)
        {
            Resolver.Log.Info($"Error handling received message: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handle desired properties PATCH (partial update)
    /// </summary>
    private void HandleDesiredPropertiesPatch(string payload)
    {
        try
        {
            Resolver.Log.Info($"Desired properties PATCH received: {payload}");

            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            var desired = new DeviceTwinProperties.Desired();
            int? version = null;

            if (root.TryGetProperty("telemetry_interval_seconds", out var intervalProp))
            {
                desired.TelemetryIntervalSeconds = intervalProp.GetInt32();
            }

            if (root.TryGetProperty("$version", out var versionProp))
            {
                version = versionProp.GetInt32();
                desired.Version = version.Value;
            }

            // Raise event to notify listeners (e.g., MeadowApp)
            DesiredPropertiesReceived?.Invoke(this, 
                new DeviceTwinDesiredPropertiesEventArgs(desired, version));

            Resolver.Log.Info($"Desired properties processed (version={version})");
        }
        catch (Exception ex)
        {
            Resolver.Log.Info($"Error parsing desired properties: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle twin GET or PATCH response
    /// </summary>
    private void HandleTwinResponse(string topic, string payload)
    {
        try
        {
            // Extract status code from topic: $iothub/twin/res/{status}/?$rid={request id}
            var parts = topic.Split('/');
            if (parts.Length >= 4 && int.TryParse(parts[3], out var statusCode))
            {
                if (statusCode == 200)
                {
                    Resolver.Log.Info($"Twin response success (200)");

                    // Parse full twin document if this is a GET response
                    if (payload.Length > 0)
                    {
                        using var document = JsonDocument.Parse(payload);
                        var root = document.RootElement;

                        // Extract desired properties from full twin
                        if (root.TryGetProperty("desired", out var desiredElement))
                        {
                            var desiredJson = desiredElement.GetRawText();
                            HandleDesiredPropertiesPatch(desiredJson);
                        }
                    }
                }
                else
                {
                    Resolver.Log.Info($"Twin response error: {statusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            Resolver.Log.Info($"Error handling twin response: {ex.Message}");
        }
    }

    /// <summary>
    /// Update reported properties of the device twin
    /// </summary>
    public async Task UpdateReportedPropertiesAsync(DeviceTwinProperties.Reported reportedProperties)
    {
        if (!await EnsureConnectedAsync())
        {
            throw new InvalidOperationException("IoT Hub connection is not available.");
        }

        try
        {
            var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var patchTopic = $"$iothub/twin/PATCH/properties/reported/?$rid={requestId}";

            // Build JSON payload using StringBuilder for efficiency
            var json = new StringBuilder();
            json.Append('{');

            var properties = new List<string>();

            if (reportedProperties.AirTemperature.HasValue)
            {
                properties.Add(string.Format(CultureInfo.InvariantCulture, 
                    "\"air_temperature\":{0}", reportedProperties.AirTemperature.Value));
            }

            if (reportedProperties.Meat1Temperature.HasValue)
            {
                properties.Add(string.Format(CultureInfo.InvariantCulture, 
                    "\"meat1_temperature\":{0}", reportedProperties.Meat1Temperature.Value));
            }

            if (reportedProperties.Meat2Temperature.HasValue)
            {
                properties.Add(string.Format(CultureInfo.InvariantCulture, 
                    "\"meat2_temperature\":{0}", reportedProperties.Meat2Temperature.Value));
            }

            if (!string.IsNullOrWhiteSpace(reportedProperties.DeviceStatus))
            {
                properties.Add($"\"device_status\":\"{reportedProperties.DeviceStatus}\"");
            }

            if (reportedProperties.TelemetryIntervalSeconds.HasValue)
            {
                properties.Add($"\"telemetry_interval_seconds\":{reportedProperties.TelemetryIntervalSeconds.Value}");
            }

            if (reportedProperties.LastUpdate.HasValue)
            {
                properties.Add($"\"last_update\":\"{reportedProperties.LastUpdate.Value:yyyy-MM-ddTHH:mm:ss.fffZ}\"");
            }

            json.Append(string.Join(",", properties));
            json.Append('}');

            var payload = json.ToString();
            Resolver.Log.Info($"Updating reported properties: {payload}");

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(patchTopic)
                .WithPayload(payload)
                .Build();

            using var publishCts = new CancellationTokenSource(PublishTimeout);
            await mqttClient.PublishAsync(message, publishCts.Token);

            Resolver.Log.Info($"Reported properties updated (rid={requestId})");
        }
        catch (Exception ex)
        {
            Resolver.Log.Info($"Error updating reported properties: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        mqttClient?.Dispose();
        _connectionLock.Dispose();
    }
}