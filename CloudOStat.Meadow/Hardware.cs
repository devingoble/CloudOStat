using CloudOStat.Meadow;

using Meadow.Devices;
using Meadow.Foundation.Displays.Lcd;
using Meadow.Foundation.Leds;
using Meadow.Hardware;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Meadow.Peripherals.Leds.IRgbLed;

namespace CloudOStat.LocalHardware
{
    public class Hardware
    {
        public IDigitalOutputPort HeaterRelay { get; private set; }
        public MAX31855 AirSensor { get; private set; }
        public MAX31855 MeatSensor1 { get; private set; }
        public MAX31855 MeatSensor2 { get; private set; }
        public CharacterDisplay Display { get; private set; }
        public RgbPwmLed OnboardLed { get; private set; }

        public Hardware(F7Micro device)
        {
            var spiBus = device.CreateSpiBus();
            var pin0 = device.CreateDigitalOutputPort(device.Pins.D00);
            var pin1 = device.CreateDigitalOutputPort(device.Pins.D01);
            var pin2 = device.CreateDigitalOutputPort(device.Pins.D02);
            HeaterRelay = device.CreateDigitalOutputPort(device.Pins.D11);
            AirSensor = new MAX31855(spiBus, pin0);
            MeatSensor1 = new MAX31855(spiBus, pin1);
            MeatSensor2 = new MAX31855(spiBus, pin2);
            Display = new CharacterDisplay(
                device,
                pinRS: device.Pins.D05,
                pinE: device.Pins.D06,
                pinD4: device.Pins.D07,
                pinD5: device.Pins.D08,
                pinD6: device.Pins.D09,
                pinD7: device.Pins.D10,
                rows: 4,
                columns: 20);

            OnboardLed = new RgbPwmLed(device: device,
                redPwmPin: device.Pins.OnboardLedRed,
                greenPwmPin: device.Pins.OnboardLedGreen,
                bluePwmPin: device.Pins.OnboardLedBlue,
                3.3f, 3.3f, 3.3f,
                CommonType.CommonAnode);
        }
    }
}
