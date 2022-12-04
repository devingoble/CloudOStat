using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Microsoft.Azure.Devices.Client;
//using Microsoft.Azure.Devices.Shared;

namespace CloudOStat.LocalHardware
{
    public class AzureConnection
    {
        private readonly string _deviceId;
        private readonly string _connectionString;
        //private readonly DeviceClient _device;

        public AzureConnection(string connectionString)
        {
            _connectionString = connectionString;
            //_device = DeviceClient.CreateFromConnectionString(_connectionString);
        }

        //public async Task Connect()
        //{
        //    await _device.OpenAsync();
        //}
    }
}
