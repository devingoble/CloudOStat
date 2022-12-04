using System;
using System.Net.NetworkInformation;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation;
using Meadow.Foundation.Displays.Lcd;
using Meadow.Foundation.Leds;
using Meadow.Gateway.WiFi;

using static Meadow.Peripherals.Leds.IRgbLed;
using System.IO;
using Meadow.Foundation.Controllers.Pid;
using System.Threading.Tasks;
using System.Text.Json;

namespace CloudOStat.LocalHardware
{
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        AzureConnection _azure;
        Hardware _hardware;
        //Settings _settings;
        bool _isConnected = false;
        HeatingElementController _controller;

        public MeadowApp()
        {
            WaitForWifi();

            _hardware = new Hardware(Device);
            //_controller = new HeatingElementController(_hardware.AirSensor, _hardware.HeaterRelay);

            //while (_settings == null)
            //{
            //    _settings = ReadSettings();

            //    if (_settings == null)
            //    {
            //        _hardware.Display.WriteLine("No settings found", 1);
            //        _hardware.Display.WriteLine("Retry in 5 seconds", 2);

            //        Thread.Sleep(10000);
            //    }
            //}

            //Console.WriteLine("Wifi: " + _settings.WiFiSSID);
            //Console.WriteLine("Key: " + _settings.WiFiKey);

            //while (!_isConnected)
            //{
            //    _isConnected = InitWifi(_settings);
            //}

            //_azure = new AzureConnection(_settings.AzureConnectionString);
            //_azure.Connect();

            //_pid = new StandardPidController();

            //_controller.StartCook(_settings.DefaultTemperature, _settings.UpdateInterval);

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

                Thread.Sleep(5000);
            }
        }

        void WaitForWifi()
        {
            if (Device.WiFiAdapter.IsConnected)
            {
                Console.WriteLine("WiFi adapter already connected.");
            }
            else
            {
                Console.WriteLine("WiFi adapter not connected.");

                Device.WiFiAdapter.WiFiConnected += (s, e) => {
                    Console.WriteLine("WiFi adapter connected.");
                };
            }
        }

        private void DisplayTemperatures(double target, double airValue, double meat1Value, double meat2Value, string status)
        {
            var airLabel = $"Act:{airValue} - Tgt:{target}";
            var meat1Label = $"Meat 1:{meat1Value}";
            var meat2Label = $"Meat 2:{meat2Value}";
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