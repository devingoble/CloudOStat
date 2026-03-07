using CloudOStat.App.Web.Components;
using CloudOStat.App.Shared.Services;
using CloudOStat.App.Web.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;
using WebClientDeviceControlService = CloudOStat.App.Web.Client.Services.DeviceControlService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddMudServices();
builder.Services.AddSingleton<NavigationService>();

// Add device-specific services used by the CloudOStat.App.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
});

builder.Services.AddScoped<IDeviceControlService>(sp =>
    new WebClientDeviceControlService(sp.GetRequiredService<HttpClient>()));

// Add controllers for API endpoints
builder.Services.AddControllers();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// Map API controllers
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(CloudOStat.App.Shared._Imports).Assembly,
        typeof(CloudOStat.App.Web.Client._Imports).Assembly);

app.Run();
