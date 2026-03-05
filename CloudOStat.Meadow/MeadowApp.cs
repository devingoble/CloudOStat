using System;
using System.Net.NetworkInformation;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Units;

using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using Meadow.Hardware;
using CloudOStat.Controllers;
using System.Collections.Generic;

namespace CloudOStat.LocalHardware
{
    public class MeadowApp : App<F7FeatherV1>
    {
        HardwarePackage _hardware;
        HeatingElementController _controller;
        IIoTHubController _iotHubController;

        // Configurable IoT Hub send interval (in milliseconds) - can be updated via Device Twin
        private int _iotHubSendIntervalMs = 20000; // Default: 20 seconds
        const int DISPLAY_REFRESH_INTERVAL_MS = 5000; // 5 seconds
        const int MAX_BATCH_SIZE = 100;
        const int MIN_TELEMETRY_INTERVAL_SECONDS = 5;
        const int MAX_TELEMETRY_INTERVAL_SECONDS = 300;

        // Collection to store readings for batching
        private readonly List<TemperatureReading> _readingsBatch = new List<TemperatureReading>();

        public async override Task Initialize()
        {
            _hardware = new HardwarePackage(Device);

            await InitWiFi();

            _iotHubController = new IoTHubMqttController();
            
            // Subscribe to Device Twin desired properties updates
            _iotHubController.DesiredPropertiesReceived += OnDesiredPropertiesReceived;
            
            await InitializeIoTHub();
        }

        public async override Task Run()
        {
            DateTime lastIoTHubSend = DateTime.MinValue;
            string lastStatus = "";
            double lastAirValue = 0;
            double lastMeat1Value = 0;
            double lastMeat2Value = 0;

            while (true)
            {
                var airValue = _hardware.AirSensor.GetProbeTemperatureDataFahrenheit();
                var meat1Value = _hardware.MeatSensor1.GetProbeTemperatureDataFahrenheit();
                var meat2Value = _hardware.MeatSensor2.GetProbeTemperatureDataFahrenheit();
                string status = "";
                string errorMessage = "";

                try
                {
                    airValue = _hardware.AirSensor.GetProbeTemperatureDataFahrenheit();
                }
                catch (InvalidOperationException ex)
                {
                    status = "air:" + ex.Message;
                    errorMessage = status;
                }

                try
                {
                    meat1Value = _hardware.MeatSensor1.GetProbeTemperatureDataFahrenheit();
                }
                catch (InvalidOperationException ex)
                {
                    status += " 1:" + ex.Message;
                    errorMessage = string.IsNullOrWhiteSpace(errorMessage) ? status : $"{errorMessage} {status}";
                }

                try
                {
                    meat2Value = _hardware.MeatSensor2.GetProbeTemperatureDataFahrenheit();
                }
                catch (InvalidOperationException ex)
                {
                    status += " 2:" + ex.Message;
                    errorMessage = string.IsNullOrWhiteSpace(errorMessage) ? status : $"{errorMessage} {status}";
                }

                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    if (airValue <= 223)
                    {
                        _hardware.HeaterRelay.State = true;
                        status = "Heating";
                        _hardware.OnboardLed.SetColor(Color.Red);
                    }
                    else if (airValue >= 227)
                    {
                        _hardware.HeaterRelay.State = false;
                        status = "Over";
                        _hardware.OnboardLed.SetColor(Color.Blue);
                    }
                    else
                    {
                        _hardware.HeaterRelay.State = false;
                        status = "On Temp";
                        _hardware.OnboardLed.SetColor(Color.Green);
                    }

                    // Add reading to batch collection
                    var reading = new TemperatureReading(
                        DateTime.UtcNow,
                        airValue,
                        meat1Value,
                        meat2Value,
                        status
                    );
                    _readingsBatch.Add(reading);
                }

                var timeSinceLastSend = DateTime.UtcNow - lastIoTHubSend;
                var shouldSendBatch = _readingsBatch.Count > 0 &&
                    (timeSinceLastSend.TotalMilliseconds >= _iotHubSendIntervalMs || _readingsBatch.Count >= MAX_BATCH_SIZE);

                if (shouldSendBatch)
                {
                    try
                    {
                        // Send batch telemetry
                        await _iotHubController.SendBatchEnvironmentalReadings(_readingsBatch);
                        Resolver.Log.Info($"IoT Hub batch sent with {_readingsBatch.Count} readings. Next batch in {_iotHubSendIntervalMs / 1000} seconds.");

                        // Update reported properties with latest readings
                        var reportedProperties = new DeviceTwinProperties.Reported
                        {
                            AirTemperature = lastAirValue,
                            Meat1Temperature = lastMeat1Value,
                            Meat2Temperature = lastMeat2Value,
                            DeviceStatus = lastStatus,
                            TelemetryIntervalSeconds = _iotHubSendIntervalMs / 1000,
                            LastUpdate = DateTime.UtcNow
                        };

                        await _iotHubController.UpdateReportedPropertiesAsync(reportedProperties);
                        Resolver.Log.Info("Device Twin reported properties updated");

                        _readingsBatch.Clear();
                        lastIoTHubSend = DateTime.UtcNow;
                    }
                    catch (InvalidOperationException ex)
                    {
                        Resolver.Log.Info($"IoT Hub send failed: {ex.Message}");
                        errorMessage = string.IsNullOrWhiteSpace(errorMessage)
                            ? $"IoT Hub: {ex.Message}"
                            : $"{errorMessage} IoT Hub: {ex.Message}";
                    }
                    catch (MQTTnet.Exceptions.MqttCommunicationException ex)
                    {
                        Resolver.Log.Info($"IoT Hub send failed: {ex.Message}");
                        errorMessage = string.IsNullOrWhiteSpace(errorMessage)
                            ? $"IoT Hub: {ex.Message}"
                            : $"{errorMessage} IoT Hub: {ex.Message}";
                    }
                    catch (MQTTnet.Exceptions.MqttProtocolViolationException ex)
                    {
                        Resolver.Log.Info($"IoT Hub send failed: {ex.Message}");
                        errorMessage = string.IsNullOrWhiteSpace(errorMessage)
                            ? $"IoT Hub: {ex.Message}"
                            : $"{errorMessage} IoT Hub: {ex.Message}";
                    }
                }

                // Track last values for reported properties
                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    lastStatus = status;
                    lastAirValue = airValue;
                    lastMeat1Value = meat1Value;
                    lastMeat2Value = meat2Value;
                }

                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    DisplayTemperatures(225, airValue, meat1Value, meat2Value, status);
                }
                else
                {
                    DisplayError(errorMessage);
                }

                await Task.Delay(DISPLAY_REFRESH_INTERVAL_MS);
            }
        }

        async Task InitWiFi()
        {
            Resolver.Log.Info("Init wifi...");

            _hardware.Display.ClearLines();
            _hardware.Display.Write("Init WiFi...");

            var wifi = Resolver.Device.NetworkAdapters.Primary<IWiFiNetworkAdapter>();

            if (wifi.IsConnected)
            {
                Resolver.Log.Info($"Already connected to WiFi - IP Address: {wifi.IpAddress}");
                _hardware.Display.ClearLines();
                _hardware.Display.WriteLine("WiFi Connected", 0);
                _hardware.Display.WriteLine(wifi.IpAddress.ToString(), 1);
            }
            else
            {
                Resolver.Log.Info($"Not connected to WiFi yet. MAC: {wifi.MacAddress}");
                _hardware.Display.WriteLine("Waiting for WiFi...", 0);
            }

            wifi.NetworkConnecting += (sender) =>
            {
                Resolver.Log.Info("Network connecting...");
                _hardware.Display.ClearLines();
                _hardware.Display.Write("Network Connecting...");
            };

            wifi.NetworkConnected += (sender, args) =>
            {
                Resolver.Log.Info($"Joined network - IP Address: {args.IpAddress}");
                _hardware.Display.ClearLines();
                _hardware.Display.WriteLine("Joined network:", 0);
                _hardware.Display.WriteLine("IP Address:", 1);
                _hardware.Display.WriteLine(args.IpAddress.ToString(), 2);
            };

            wifi.NetworkDisconnected += (sender, args) =>
            {
                Resolver.Log.Info("Network disconnected.");
                _hardware.Display.ClearLines();
                _hardware.Display.Write("Disconnected");
            };

            wifi.NetworkConnectFailed += (sender) =>
            {
                Resolver.Log.Info("Could not connect to network");
                _hardware.Display.ClearLines();
                _hardware.Display.Write("Connect failed");
            };
        }

        private async Task InitializeIoTHub()
        {
            while (!_iotHubController.isAuthenticated)
            {
                _hardware.Display.ClearLines();
                _hardware.Display.Write("Connecting to Azure...");

                bool authenticated = await _iotHubController.Initialize();

                if (authenticated)
                {
                    Resolver.Log.Info("Authenticated");
                    _hardware.Display.ClearLines();
                    _hardware.Display.Write("Connected");
                }
                else
                {
                    Resolver.Log.Info("Not Authenticated");
                    _hardware.Display.ClearLines();
                    _hardware.Display.Write("Could not connect");
                }


                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        private void DisplayTemperatures(double target, double airValue, double meat1Value, double meat2Value, string status)
        {
            var airLabel = $"Air:{Math.Round(airValue, 2)} - Tgt:{target}";
            var meat1Label = $"Meat 1:{Math.Round(meat1Value, 2)}";
            var meat2Label = $"Meat 2:{Math.Round(meat2Value, 2)}";
            var statusLabel = $"Status:{status}";

            _hardware.Display.WriteLine(airLabel, 0);
            _hardware.Display.WriteLine(meat1Label, 1);
            _hardware.Display.WriteLine(meat2Label, 2);
            _hardware.Display.WriteLine(statusLabel, 3);

            Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} {airLabel} {meat1Label} {meat2Label} {statusLabel}");
        }

        private void DisplayError(string errorMessage)
        {
            _hardware.Display.ClearLines();
            _hardware.Display.WriteLine("Error", 0);

            var message = string.IsNullOrWhiteSpace(errorMessage) ? "Unknown error" : errorMessage;
            var maxLineLength = 20;
            var maxLines = 3;
            var lineIndex = 1;

            for (var i = 0; i < message.Length && lineIndex <= maxLines; i += maxLineLength, lineIndex++)
            {
                var length = Math.Min(maxLineLength, message.Length - i);
                var line = message.Substring(i, length);
                _hardware.Display.WriteLine(line, (byte)lineIndex);
            }
        }

        /// <summary>
        /// Handle Device Twin desired properties updates from Azure IoT Hub
        /// </summary>
        private void OnDesiredPropertiesReceived(object? sender, DeviceTwinDesiredPropertiesEventArgs e)
        {
            try
            {
                Resolver.Log.Info($"Desired properties received (version={e.Version})");

                // Handle telemetry interval updates
                if (e.DesiredProperties.TelemetryIntervalSeconds.HasValue)
                {
                    var desiredInterval = e.DesiredProperties.TelemetryIntervalSeconds.Value;

                    // Validate interval is within acceptable range
                    if (desiredInterval < MIN_TELEMETRY_INTERVAL_SECONDS || desiredInterval > MAX_TELEMETRY_INTERVAL_SECONDS)
                    {
                        Resolver.Log.Info($"Telemetry interval {desiredInterval}s is out of range ({MIN_TELEMETRY_INTERVAL_SECONDS}-{MAX_TELEMETRY_INTERVAL_SECONDS}s). Ignoring.");
                        
                        _hardware.Display.ClearLines();
                        _hardware.Display.WriteLine("Invalid interval", 0);
                        _hardware.Display.WriteLine($"Must be {MIN_TELEMETRY_INTERVAL_SECONDS}-{MAX_TELEMETRY_INTERVAL_SECONDS}s", 1);
                        
                        return;
                    }

                    var previousInterval = _iotHubSendIntervalMs / 1000;
                    _iotHubSendIntervalMs = desiredInterval * 1000;

                    Resolver.Log.Info($"Telemetry interval updated: {previousInterval}s → {desiredInterval}s");

                    // Show update on display
                    _hardware.Display.ClearLines();
                    _hardware.Display.WriteLine("Interval updated", 0);
                    _hardware.Display.WriteLine($"Was: {previousInterval}s", 1);
                    _hardware.Display.WriteLine($"Now: {desiredInterval}s", 2);

                    // Display will be replaced by temperature readings in the next loop iteration
                }
            }
            catch (Exception ex)
            {
                Resolver.Log.Info($"Error processing desired properties: {ex.Message}");
            }
        }

        private Settings ReadSettings()
        {
            _hardware.Display.WriteLine("Reading config...", 0);

            if (File.Exists(@"/meadow0/settings.json"))
            {
                return null; //JsonSerializer.Deserialize<Settings>(File.ReadAllText(@"/meadow0/settings.json"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            }
            else
            {
                return null;
            }
        }
    }
}