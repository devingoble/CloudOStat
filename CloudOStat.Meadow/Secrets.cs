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
    /// example "SharedAccessSignature sr=MeadowIoTHub ..... "
    /// </summary>
    public const string SAS_TOKEN = "SharedAccessSignature sr=usw-iot-cloudostat.azure-devices.net%2Fdevices%2Fcloudostat-meadow&sig=W6IE%2FtYMl%2FpcyiNYXkq3uLtyLyhTYlCkAzxxJ5RIetY%3D&se=1715905722";
}