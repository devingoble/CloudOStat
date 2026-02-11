namespace CloudOStat.Common.Drivers
{
    public interface IMAX31855
    {
        double GetCorrectedCelcius();
        double GetCorrectedFahrenheit();
        double GetInternalTemperatureDataCelcius();
        double GetInternalTemperatureDataFahrenheit();
        double GetProbeTemperatureDataCelsius();
        double GetProbeTemperatureDataFahrenheit();
    }
}