using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudOStat.LocalHardware
{
    public class Settings
    {
        public string WiFiSSID { get; private set; }
        public string WiFiKey { get; private set; }
        public string AzureDeviceId { get; private set; }
        public string AzureConnectionString { get; private set; }
        public int DefaultTemperature { get; private set; }
        public int UpdateInterval { get; }

        public Settings(string wifiSSID, string wifiKey, string azureDeviceId, string azureConnectionString, int defaultTemperature, int updateInterval)
        {
            WiFiSSID = wifiSSID;
            WiFiKey = wifiKey;
            AzureDeviceId = azureDeviceId;
            AzureConnectionString = azureConnectionString;
            DefaultTemperature = defaultTemperature;
            UpdateInterval = updateInterval;
        }
    }
}
