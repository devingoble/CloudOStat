using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CloudOStat.App.Shared.Services;
using CloudOStat.App.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add device-specific services used by the CloudOStat.App.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

await builder.Build().RunAsync();
