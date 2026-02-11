using CloudOStat.Common.Drivers;

using Meadow.Foundation.Displays.Lcd;
using Meadow.Foundation.Leds;
using Meadow.Hardware;
using Meadow.Peripherals.Displays;
using Meadow.Peripherals.Leds;

namespace CloudOStat.LocalHardware
{
    public interface IHardwarePackage
    {
        IMAX31855 AirSensor { get; }
        ITextDisplay Display { get; }
        IDigitalOutputPort HeaterRelay { get; }
        IMAX31855 MeatSensor1 { get; }
        IMAX31855 MeatSensor2 { get; }
        IRgbPwmLed OnboardLed { get; }
    }
}