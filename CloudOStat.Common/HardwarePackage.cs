using CloudOStat.Common.Drivers;

using Meadow.Devices;
using Meadow.Foundation.Displays.Lcd;
using Meadow.Foundation.Leds;
using Meadow.Hardware;
using Meadow.Peripherals.Displays;
using Meadow.Peripherals.Leds;
using Meadow.Units;

namespace CloudOStat.LocalHardware
{
    public class HardwarePackage : IHardwarePackage
    {
        public IDigitalOutputPort HeaterRelay { get; private set; }
        public IMAX31855 AirSensor { get; private set; }
        public IMAX31855 MeatSensor1 { get; private set; }
        public IMAX31855 MeatSensor2 { get; private set; }
        public ITextDisplay Display { get; private set; }
        public IRgbPwmLed OnboardLed { get; private set; }

        public HardwarePackage(F7FeatherV1 device)
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
                pinRS: device.Pins.D05,
                pinE: device.Pins.D06,
                pinD4: device.Pins.D07,
                pinD5: device.Pins.D08,
                pinD6: device.Pins.D09,
                pinD7: device.Pins.D10,
                rows: 4,
                columns: 20);

            OnboardLed = new RgbPwmLed(
                redPwmPin: device.Pins.OnboardLedRed,
                greenPwmPin: device.Pins.OnboardLedGreen,
                bluePwmPin: device.Pins.OnboardLedBlue,
                new Voltage(3.3), new Voltage(3.3), new Voltage(3.3),
                CommonType.CommonAnode);
        }
    }
}
