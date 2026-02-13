var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.CloudOStat_App>("cloudostat-app");

builder.AddProject<Projects.CloudOStat_App_Web>("cloudostat-app-web");

builder.Build().Run();
