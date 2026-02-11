using CloudOStat.Common.Drivers;

using System;
using System.Collections.Generic;
using System.Text;

namespace CloudOStat.DesktopPrototype.Drivers
{
    public class VirtualMAX31855 : IMAX31855
    {
        public double GetCorrectedCelcius()
        {
            throw new NotImplementedException();
        }

        public double GetCorrectedFahrenheit()
        {
            throw new NotImplementedException();
        }

        public double GetInternalTemperatureDataCelcius()
        {
            throw new NotImplementedException();
        }

        public double GetInternalTemperatureDataFahrenheit()
        {
            throw new NotImplementedException();
        }

        public double GetProbeTemperatureDataCelsius()
        {
            throw new NotImplementedException();
        }

        public double GetProbeTemperatureDataFahrenheit()
        {
            var r = new Random();

            return r.Next(223, 228);
        }
    }
}
