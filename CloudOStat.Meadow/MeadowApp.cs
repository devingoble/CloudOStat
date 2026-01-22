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

namespace CloudOStat.LocalHardware
{
    public class MeadowApp : App<F7FeatherV1>
    {
        Hardware _hardware;
        HeatingElementController _controller;
        IIoTHubController _iotHubController;

        public override async Task Initialize()
        {
            _hardware = new Hardware(Device);

            InitWiFi();

            _iotHubController = new IoTHubMqttController();
            await InitializeIoTHub();
        }

        public override Task Run()
        {
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
                };

                DisplayTemperatures(225, airValue, meat1Value, meat2Value, status);
                _iotHubController.SendEnvironmentalReading(new Temperature(meat1Value, Temperature.UnitType.Fahrenheit), new Temperature(meat2Value, Temperature.UnitType.Fahrenheit), new Temperature(airValue, Temperature.UnitType.Fahrenheit));

                Thread.Sleep(5000);
            }
        }

        void InitWiFi()
        {
            Resolver.Log.Info("Init wifi...");

            _hardware.Display.ClearLines();
            _hardware.Display.Write("Init WiFi...");

            var wifi = Resolver.Device.NetworkAdapters.Primary<IWiFiNetworkAdapter>();

            if (wifi.IsConnected)
            {
                Resolver.Log.Info("Already connected to WiFi.");
            }
            else
            {
                Resolver.Log.Info("Not connected to WiFi yet.");
            }
            // connect event
            wifi.NetworkConnected += (networkAdapter, networkConnectionEventArgs) =>
            {
                Resolver.Log.Info($"Joined network - IP Address: {networkAdapter.IpAddress}");
                _hardware.Display.ClearLines();
                _hardware.Display.WriteLine("Joined network:", 0);
                _hardware.Display.WriteLine($"IP Address:", 1);
                _hardware.Display.WriteLine(networkAdapter.IpAddress.ToString(), 1);

                return;
            };
            // disconnect event
            wifi.NetworkDisconnected += (o, e) =>
            {
                Resolver.Log.Info($"Network disconnected.");

                return;
            };
        }

        private async Task InitializeIoTHub()
        {
            while (!_iotHubController.isAuthenticated)
            {
                _hardware.Display.ClearLines();
                _hardware.Display.Write("Connecting...");

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