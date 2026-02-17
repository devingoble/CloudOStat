using Meadow.Units;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CloudOStat.LocalHardware;

namespace CloudOStat.Controllers;

/// <summary>
/// You'll need to create an IoT Hub - https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal
/// Create a device within your IoT Hub
/// And then generate a SAS token - this can be done via the Azure CLI 
/// 
/// Example
/// az iot hub generate-sas-token
/// --hub-name HUB_NAME 
/// --device-id DEVICE_ID 
/// --resource-group RESOURCE_GROUP 
/// --login [Open Shared access policies -> Select iothubowner -> copy Primary connection string]
/// </summary>
internal interface IIoTHubController
{
    bool isAuthenticated { get; }

    Task<bool> Initialize();

    Task SendEnvironmentalReading(Temperature meatOne, Temperature meatTwo, Temperature air);

    Task SendBatchEnvironmentalReadings(List<TemperatureReading> readings);

    /// <summary>
    /// Updates the reported properties of the device twin with current sensor readings and status
    /// </summary>
    Task UpdateReportedPropertiesAsync(DeviceTwinProperties.Reported reportedProperties);

    /// <summary>
    /// Event raised when desired properties are received from Azure IoT Hub
    /// </summary>
    event EventHandler<DeviceTwinDesiredPropertiesEventArgs>? DesiredPropertiesReceived;
}

/// <summary>
/// Event arguments for device twin desired properties received event
/// </summary>
public class DeviceTwinDesiredPropertiesEventArgs : EventArgs
{
    public DeviceTwinProperties.Desired DesiredProperties { get; set; }
    public int? Version { get; set; }

    public DeviceTwinDesiredPropertiesEventArgs(DeviceTwinProperties.Desired desiredProperties, int? version)
    {
        DesiredProperties = desiredProperties;
        Version = version;
    }
}