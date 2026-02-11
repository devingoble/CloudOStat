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

        // Configurable IoT Hub send interval (in milliseconds)
        const int IOT_HUB_SEND_INTERVAL_MS = 20000; // 20 seconds
        const int DISPLAY_REFRESH_INTERVAL_MS = 5000; // 5 seconds

        // Collection to store readings for batching
        private readonly List<TemperatureReading> _readingsBatch = new List<TemperatureReading>();

        public async override Task Initialize()
        {
            _hardware = new HardwarePackage(Device);

            await InitWiFi();

            _iotHubController = new IoTHubMqttController();
            await InitializeIoTHub();
        }

        public override Task Run()
        {
            DateTime lastIoTHubSend = DateTime.MinValue;

            while (true)
            {
                var airValue = _hardware.AirSensor.GetProbeTemperatureDataFahrenheit();
                var meat1Value = _hardware.MeatSensor1.GetProbeTemperatureDataFahrenheit();
                var meat2Value = _hardware.MeatSensor2.GetProbeTemperatureDataFahrenheit();
                string status = "";

                try
                {
                    airValue = _hardware.AirSensor.GetProbeTemperatureDataFahrenheit();
                }
                catch (Exception ex)
                {
                    status = "air:" + ex.Message;
                }

                try
                {
                    meat1Value = _hardware.MeatSensor1.GetProbeTemperatureDataFahrenheit();
                }
                catch (Exception ex)
                {
                    status += " 1:" + ex.Message;
                }

                try
                {
                    meat2Value = _hardware.MeatSensor2.GetProbeTemperatureDataFahrenheit();
                }
                catch (Exception ex)
                {
                    status += " 2:" + ex.Message;
                }


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

                // Always refresh the display
                DisplayTemperatures(225, airValue, meat1Value, meat2Value, status);

                // Add reading to batch collection
                var reading = new TemperatureReading(
                    DateTime.UtcNow,
                    airValue,
                    meat1Value,
                    meat2Value,
                    status
                );
                _readingsBatch.Add(reading);

                // Only send to IoT Hub at the configured interval
                var timeSinceLastSend = DateTime.UtcNow - lastIoTHubSend;
                if (timeSinceLastSend.TotalMilliseconds >= IOT_HUB_SEND_INTERVAL_MS)
                {
                    // Send the entire batch
                    if (_readingsBatch.Count > 0)
                    {
                        _iotHubController.SendBatchEnvironmentalReadings(_readingsBatch);
                        
                        Resolver.Log.Info($"IoT Hub batch sent with {_readingsBatch.Count} readings. Next batch in {IOT_HUB_SEND_INTERVAL_MS / 1000} seconds.");
                        
                        // Clear the batch after sending
                        _readingsBatch.Clear();
                    }
                    
                    lastIoTHubSend = DateTime.UtcNow;
                }

                Thread.Sleep(DISPLAY_REFRESH_INTERVAL_MS);
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