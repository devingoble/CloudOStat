using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudOStat.LocalHardware
{
    public class DesiredState
    {
        public int TargetAirTemperature { get; }
        public TimeSpan TelemetryFrequency { get; }

        public DesiredState(int targetAirTemperature, TimeSpan telemetryFrequency)
        {
            TargetAirTemperature = targetAirTemperature;
            TelemetryFrequency = telemetryFrequency;
        }
    }
}
