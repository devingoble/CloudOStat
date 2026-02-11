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
    public class VirtualPackage : IHardwarePackage
    {
        public IDigitalOutputPort HeaterRelay { get; private set; }
        public IMAX31855 AirSensor { get; private set; }
        public IMAX31855 MeatSensor1 { get; private set; }
        public IMAX31855 MeatSensor2 { get; private set; }
        public ITextDisplay Display { get; private set; }
        public IRgbPwmLed OnboardLed { get; private set; }

        public VirtualPackage(Desktop device)
        {
            var D00 = device.GetPin("D00");
            var D01 = device.GetPin("D01");
            var D02 = device.GetPin("D02");
            var D05 = device.GetPin("D05");
            var D06 = device.GetPin("D06");
            var D07 = device.GetPin("D07");
            var D08 = device.GetPin("D08");
            var D09 = device.GetPin("D09");
            var D10 = device.GetPin("D10");
            var D11 = device.GetPin("D11");

            

            var spiBus = device.CreateSpiBus(3, new Frequency(4800));
            var pin0 = device.CreateDigitalOutputPort(D00);
            var pin1 = device.CreateDigitalOutputPort(D01);
            var pin2 = device.CreateDigitalOutputPort(D02);
            HeaterRelay = device.CreateDigitalOutputPort(D11);
            AirSensor = new MAX31855(spiBus, pin0);
            MeatSensor1 = new MAX31855(spiBus, pin1);
            MeatSensor2 = new MAX31855(spiBus, pin2);
            Display = new CharacterDisplay(
                pinRS: D05,
                pinE: D06,
                pinD4: D07,
                pinD5: D08,
                pinD6: D09,
                pinD7: D10,
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
