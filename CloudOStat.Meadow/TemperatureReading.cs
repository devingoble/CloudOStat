using System;

namespace CloudOStat.LocalHardware
{
    public class TemperatureReading
    {
        public DateTime Timestamp { get; set; }
        public double AirTemperature { get; set; }
        public double Meat1Temperature { get; set; }
        public double Meat2Temperature { get; set; }
        public string Status { get; set; }

        public TemperatureReading(DateTime timestamp, double airTemp, double meat1Temp, double meat2Temp, string status)
        {
            Timestamp = timestamp;
            AirTemperature = airTemp;
            Meat1Temperature = meat1Temp;
            Meat2Temperature = meat2Temp;
            Status = status;
        }
    }
}
