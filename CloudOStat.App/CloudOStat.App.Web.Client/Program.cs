using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CloudOStat.App.Shared.Services;
using CloudOStat.App.Web.Client.Services;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();
builder.Services.AddSingleton<NavigationService>();

// Add device-specific services used by the CloudOStat.App.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

// Add HttpClient for DeviceControlService
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add DeviceControlService that communicates with backend API
builder.Services.AddScoped<IDeviceControlService>(sp => new DeviceControlService(sp.GetRequiredService<HttpClient>()));

await builder.Build().RunAsync();
