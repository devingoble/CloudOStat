var builder = DistributedApplication.CreateBuilder(args);

var iotHubName = builder.AddParameter("IoTHubName", secret: true);
var iotDeviceId = builder.AddParameter("IoTDeviceId", secret: true);
var iotSharedAccessKey = builder.AddParameter("IoTSharedAccessKey", secret: true);
var iotHubPolicyName = builder.AddParameter("IoTHubPolicyName");

builder.AddProject<Projects.CloudOStat_App>("cloudostat-app");

builder.AddProject<Projects.CloudOStat_App_Web>("cloudostat-app-web")
    .WithEnvironment("IoTHub__HubName", iotHubName)
    .WithEnvironment("IoTHub__DeviceId", iotDeviceId)
    .WithEnvironment("IoTHub__SharedAccessKey", iotSharedAccessKey)
    .WithEnvironment("IoTHub__SharedAccessKeyName", iotHubPolicyName);

builder.Build().Run();
