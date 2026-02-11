namespace CloudOStat;

public class Secrets
{
    /// <summary>
    /// Name of the Azure IoT Hub created
    /// </summary>
    public const string HUB_NAME = "usw-iot-cloudostat";

    /// <summary>
    /// Name of the Azure IoT Hub created
    /// </summary>
    public const string DEVICE_ID = "cloudostat-meadow";

    /// <summary>
    /// Device connection string from Azure IoT Hub
    /// Get this from: IoT Hub -> Device management -> Devices -> [Your Device] -> Primary Connection String
    /// Format: "HostName=usw-iot-cloudostat.azure-devices.net;DeviceId=cloudostat-meadow;SharedAccessKey=YOUR_DEVICE_KEY"
    /// </summary>
    public const string CONNECTION_STRING = "";
}