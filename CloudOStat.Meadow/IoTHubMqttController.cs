using Meadow;
using Meadow.Units;

using MQTTnet;
using MQTTnet.Client;

using System;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using CloudOStat.LocalHardware;

namespace CloudOStat.Controllers;

internal class IoTHubMqttController : IIoTHubController
{
    string IOT_HUB_NAME = Secrets.HUB_NAME;
    string IOT_HUB_DEVICE_ID = Secrets.DEVICE_ID;
    string IOT_HUB_CONNECTION_STRING = Secrets.CONNECTION_STRING;

    const int SasTokenExpiryMinutes = 60;
    static readonly TimeSpan SasTokenRefreshBuffer = TimeSpan.FromMinutes(15);

    IMqttClient mqttClient;
    readonly SemaphoreSlim _connectionLock = new(1, 1);
    string _iotHubUri;
    string _username;
    DateTimeOffset _sasTokenExpiryUtc;
    int _desiredPropertiesVersion = 0;

    public bool isAuthenticated { get; private set; }

    public event EventHandler<DeviceTwinDesiredPropertiesEventArgs>? DesiredPropertiesReceived;

    public IoTHubMqttController() { }

    /// <summary>
    /// Generates a SAS token from the IoT Hub connection string
    /// </summary>
    private string GenerateSasToken(out DateTimeOffset expiryUtc, int expiryMinutes = SasTokenExpiryMinutes)
    {
        try
        {
            // Parse connection string
            var parts = IOT_HUB_CONNECTION_STRING.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            string hostName = null;
            string sharedAccessKey = null;
            string deviceId = null;

            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                if (trimmedPart.StartsWith("HostName="))
                    hostName = trimmedPart.Substring("HostName=".Length);
                else if (trimmedPart.StartsWith("SharedAccessKey="))
                    sharedAccessKey = trimmedPart.Substring("SharedAccessKey=".Length);
                else if (trimmedPart.StartsWith("DeviceId="))
                    deviceId = trimmedPart.Substring("DeviceId=".Length);
            }

            if (string.IsNullOrEmpty(hostName))
            {
                throw new InvalidOperationException("Connection string is missing HostName");
            }

            if (string.IsNullOrEmpty(sharedAccessKey))
            {
                throw new InvalidOperationException("Connection string is missing SharedAccessKey");
            }

            // Use DeviceId from connection string if available, otherwise use the configured one
            var effectiveDeviceId = !string.IsNullOrEmpty(deviceId) ? deviceId : IOT_HUB_DEVICE_ID;

            Resolver.Log.Info($"DEBUG: Parsed HostName: {hostName}");
            Resolver.Log.Info($"DEBUG: Using DeviceId: {effectiveDeviceId}");

            // Create resource URI for the device
            var resourceUri = $"{hostName}/devices/{effectiveDeviceId}";
            
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
            var keyBytes = Convert.FromBase64String(sharedAccessKey);
            using (var hmac = new HMACSHA256(keyBytes))
            {
                var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
                var signature = Convert.ToBase64String(signatureBytes);
                Resolver.Log.Info($"DEBUG: Signature generated successfully");

                // Build the SAS token for device authentication
                // Note: encodedResourceUri is already URL-encoded, signature needs encoding
                var sasToken = $"SharedAccessSignature sr={encodedResourceUri}&sig={Uri.EscapeDataString(signature)}&se={expiryTime}";
                Resolver.Log.Info($"DEBUG: SAS token generated successfully");
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
        Resolver.Log.Info("Generating SAS token...");
        var sasToken = GenerateSasToken(out var expiryUtc);
        _sasTokenExpiryUtc = expiryUtc;

        Resolver.Log.Info("Creating MQTT options ...");
        return new MqttClientOptionsBuilder()
            .WithClientId(IOT_HUB_DEVICE_ID)
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

    private async Task<bool> EnsureConnectedAsync()
    {
        if (mqttClient != null && mqttClient.IsConnected)
        {
            if (IsSasTokenExpiringSoon())
            {
                Resolver.Log.Info("SAS token is expiring soon, reconnecting...");
                await mqttClient.DisconnectAsync();
                isAuthenticated = false;
            }
            else
            {
                return true;
            }
        }

        await _connectionLock.WaitAsync();
        try
        {
            if (mqttClient == null)
            {
                Resolver.Log.Info("Create connection factory...");
                var factory = new MqttFactory();

                Resolver.Log.Info("Create MQTT client...");
                mqttClient = factory.CreateMqttClient();
                mqttClient.DisconnectedAsync += args =>
                {
                    isAuthenticated = false;
                    Resolver.Log.Info($"MQTT disconnected: {args.ReasonString}");
                    return Task.CompletedTask;
                };

                await SetupMessageHandlerAsync();
            }

            if (string.IsNullOrWhiteSpace(_iotHubUri))
            {
                _iotHubUri = $"{IOT_HUB_NAME}.azure-devices.net";
            }

            if (string.IsNullOrWhiteSpace(_username))
            {
                _username = $"{IOT_HUB_NAME}.azure-devices.net/{IOT_HUB_DEVICE_ID}/?api-version=2021-04-12";
            }

            var options = BuildMqttOptions();

            Resolver.Log.Info("Azure Connecting...");
            await mqttClient.ConnectAsync(options, CancellationToken.None);

            // Subscribe to device twin topics after successful connection
            await SubscribeToDesiredPropertiesAsync();

            isAuthenticated = true;
            return true;
        }
        catch (Exception ex)
        {
            Resolver.Log.Info($"Azure Error: {ex.Message}");
            isAuthenticated = false;
            return false;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task<bool> Initialize()
    {
        _iotHubUri = $"{IOT_HUB_NAME}.azure-devices.net";
        _username = $"{IOT_HUB_NAME}.azure-devices.net/{IOT_HUB_DEVICE_ID}/?api-version=2021-04-12";

        return await EnsureConnectedAsync();
    }

    public async Task SendEnvironmentalReading(Temperature meatOne, Temperature meatTwo, Temperature air)
    {
        try
        {
            if (!await EnsureConnectedAsync())
            {
                return;
            }

            Resolver.Log.Info("Create payload");

            string messagePayload = $"" +
                $"{{" +
                $"\"meat_one\":{meatOne.Fahrenheit}," +
                $"\"meat_two\":{meatTwo.Fahrenheit}," +
                $"\"air\":{air.Fahrenheit}" +
                $"}}";

            Resolver.Log.Info("Create message");
            Resolver.Log.Info(messagePayload);
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic($"devices/{IOT_HUB_DEVICE_ID}/messages/events/")
                .WithPayload(messagePayload)
                .Build();

            await mqttClient.PublishAsync(mqttMessage, new System.Threading.CancellationToken());

            Resolver.Log.Info($"*** MQTT - DATA SENT - Meat One - {meatOne.Fahrenheit}, Meat Two - {meatTwo.Fahrenheit}, Air - {air.Fahrenheit} ***");
        }
        catch (Exception ex)
        {
            Resolver.Log.Info($"-- D2C Error - {ex.Message} --");
        }
    }

    public async Task SendBatchEnvironmentalReadings(List<TemperatureReading> readings)
    {
        try
        {
            if (!await EnsureConnectedAsync())
            {
                return;
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

                readingsJson.Append("{");
                readingsJson.Append($"\"timestamp\":\"{reading.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}\",");
                readingsJson.Append($"\"air\":{reading.AirTemperature},");
                readingsJson.Append($"\"meat_one\":{reading.Meat1Temperature},");
                readingsJson.Append($"\"meat_two\":{reading.Meat2Temperature},");
                readingsJson.Append($"\"status\":\"{reading.Status}\"");
                readingsJson.Append("}");
            }

            readingsJson.Append("]}");

            string messagePayload = readingsJson.ToString();
            Resolver.Log.Info("Create batch message");
            Resolver.Log.Info($"Batch payload size: {messagePayload.Length} bytes");

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic($"devices/{IOT_HUB_DEVICE_ID}/messages/events/")
                .WithPayload(messagePayload)
                .Build();

            await mqttClient.PublishAsync(mqttMessage, new System.Threading.CancellationToken());

            Resolver.Log.Info($"*** MQTT - BATCH SENT - {readings.Count} readings ***");
        }
        catch (Exception ex)
        {
            Resolver.Log.Info($"-- Batch D2C Error - {ex.Message} --");
        }
    }

    private async Task SubscribeToDesiredPropertiesAsync()
    {
        try
        {
            if (mqttClient == null || !mqttClient.IsConnected)
            {
                return;
            }

            Resolver.Log.Info("Subscribing to device twin desired properties topic...");
            
            // Subscribe to desired property changes
            var topicFilter = new MqttTopicFilterBuilder()
                .WithTopic($"$iothub/twin/PATCH/properties/desired/#")
                .Build();

            await mqttClient.SubscribeAsync(topicFilter);
            Resolver.Log.Info("Successfully subscribed to desired properties topic");
        }
        catch (Exception ex)
        {
            Resolver.Log.Error($"Failed to subscribe to desired properties: {ex.Message}");
        }
    }

    private async Task SetupMessageHandlerAsync()
    {
        if (mqttClient == null)
        {
            return;
        }

        mqttClient.ApplicationMessageReceivedAsync += async args =>
        {
            try
            {
                if (args.ApplicationMessage.Topic.StartsWith("$iothub/twin/PATCH/properties/desired/"))
                {
                    await HandleDesiredPropertyUpdateAsync(args.ApplicationMessage);
                }
            }
            catch (Exception ex)
            {
                Resolver.Log.Error($"Error handling MQTT message: {ex.Message}");
            }
        };
    }

    private async Task HandleDesiredPropertyUpdateAsync(MqttApplicationMessage message)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(message.PayloadSegment);
            Resolver.Log.Info($"Received desired properties: {payload}");

            var jsonDocument = JsonDocument.Parse(payload);
            var root = jsonDocument.RootElement;

            // Extract the desired properties
            var desired = new DeviceTwinProperties.Desired();

            if (root.TryGetProperty("telemetry_interval_seconds", out var intervalProp))
            {
                if (intervalProp.TryGetInt32(out int interval))
                {
                    desired.TelemetryIntervalSeconds = interval;
                }
            }

            if (root.TryGetProperty("$version", out var versionProp))
            {
                if (versionProp.TryGetInt32(out int version))
                {
                    desired.Version = version;
                    _desiredPropertiesVersion = version;
                }
            }

            Resolver.Log.Info($"Parsed desired properties - Telemetry interval: {desired.TelemetryIntervalSeconds}s, Version: {desired.Version}");

            // Raise the event for the application to handle
            DesiredPropertiesReceived?.Invoke(this, new DeviceTwinDesiredPropertiesEventArgs(desired, desired.Version));
        }
        catch (Exception ex)
        {
            Resolver.Log.Error($"Failed to parse desired properties: {ex.Message}");
        }
    }

    public async Task UpdateReportedPropertiesAsync(DeviceTwinProperties.Reported reportedProperties)
    {
        try
        {
            if (!await EnsureConnectedAsync())
            {
                return;
            }

            // Increment version for reported properties
            var reportedWithVersion = new
            {
                air_temperature = reportedProperties.AirTemperature,
                meat1_temperature = reportedProperties.Meat1Temperature,
                meat2_temperature = reportedProperties.Meat2Temperature,
                device_status = reportedProperties.DeviceStatus,
                telemetry_interval_seconds = reportedProperties.TelemetryIntervalSeconds,
                last_update = reportedProperties.LastUpdate?.ToString("O")
            };

            var jsonPayload = JsonSerializer.Serialize(reportedWithVersion);
            Resolver.Log.Info($"Updating reported properties: {jsonPayload}");

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic($"$iothub/twin/PATCH/properties/reported/")
                .WithPayload(jsonPayload)
                .Build();

            await mqttClient.PublishAsync(mqttMessage, CancellationToken.None);

            Resolver.Log.Info($"*** MQTT - REPORTED PROPERTIES SENT ***");
        }
        catch (Exception ex)
        {
            Resolver.Log.Error($"-- Failed to update reported properties - {ex.Message} --");
        }
    }
}