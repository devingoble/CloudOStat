# IoT Hub Configuration Setup Guide

## Overview

This guide explains how to configure Azure IoT Hub credentials across all CloudOStat projects (MAUI, Blazor Web, and WebAssembly).

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Configuration Strategy                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  MAUI App (Native)                                             │
│  └─ appsettings.json (embedded resource)                      │
│  └─ appsettings.Debug.json (optional, dev overrides)          │
│  └─ appsettings.Release.json (optional, prod overrides)       │
│                                                                 │
│  Blazor Web (Backend)                                          │
│  └─ appsettings.json (repository, shared defaults)            │
│  └─ appsettings.Development.json (dev environment)            │
│  └─ appsettings.Production.json (production)                  │
│  └─ User Secrets (safe for local dev credentials)             │
│                                                                 │
│  Blazor WASM (Client)                                          │
│  └─ Calls backend API (no credentials exposed to browser)     │
│  └─ Backend handles all Azure authentication                  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Configuration Files

### 1. MAUI App Configuration

#### `appsettings.json` (Shared Base Configuration)
**Location:** `CloudOStat.App/CloudOStat.App/appsettings.json`

Default values for all environments. Embedded in the app bundle.

```json
{
  "IoTHub": {
    "HubName": "usw-iot-cloudostat",
    "DeviceId": "cloudostat-meadow",
    "SharedAccessKey": ""
  }
}
```

#### `appsettings.Debug.json` (Development Overrides)
**Location:** `CloudOStat.App/CloudOStat.App/appsettings.Debug.json`

Used when running in Debug configuration. Override values here for development.

```json
{
  "IoTHub": {
    "SharedAccessKey": "YOUR_DEBUG_KEY_HERE"
  }
}
```

#### `appsettings.Release.json` (Production Overrides)
**Location:** `CloudOStat.App/CloudOStat.App/appsettings.Release.json`

Used when running in Release configuration. Override values here for production.

```json
{
  "IoTHub": {
    "SharedAccessKey": "YOUR_PRODUCTION_KEY_HERE"
  }
}
```

### 2. Blazor Web Backend Configuration

#### `appsettings.json` (Shared Base Configuration)
**Location:** `CloudOStat.App/CloudOStat.App.Web/appsettings.json`

Repository default configuration.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "IoTHub": {
    "HubName": "usw-iot-cloudostat",
    "DeviceId": "cloudostat-meadow",
    "SharedAccessKey": ""
  }
}
```

#### `appsettings.Development.json` (Development Overrides)
**Location:** `CloudOStat.App/CloudOStat.App.Web/appsettings.Development.json`

Used when running locally or in development environment.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "IoTHub": {
    "HubName": "usw-iot-cloudostat",
    "DeviceId": "cloudostat-meadow",
    "SharedAccessKey": ""
  }
}
```

### 3. Blazor WASM Client Configuration

**No configuration required** - The client makes calls to the backend API. Backend handles all Azure authentication securely server-side.

## Setting Up Credentials

### Option A: Direct Configuration File (Not Recommended for Production)

#### For MAUI:
1. Open `appsettings.json` or `appsettings.Debug.json`
2. Fill in `SharedAccessKey` with your device's shared access key
3. Rebuild the app

#### For Blazor Web:
1. Open `appsettings.Development.json` or `appsettings.json`
2. Fill in `SharedAccessKey`
3. Redeploy or restart the server

### Option B: Using User Secrets (Recommended for Development)

User Secrets store sensitive credentials outside the repository, safe for local development.

#### For Blazor Web Backend:

1. **Initialize User Secrets for the project:**
   ```powershell
   cd CloudOStat.App/CloudOStat.App.Web
   dotnet user-secrets init
   ```

2. **Set the shared access key:**
   ```powershell
   dotnet user-secrets set "IoTHub:SharedAccessKey" "your-shared-access-key-here"
   ```

3. **Verify it's set:**
   ```powershell
   dotnet user-secrets list
   ```

4. **Location:** User secrets are stored in:
   - Windows: `%APPDATA%\Microsoft\UserSecrets\{project-id}\secrets.json`
   - Linux/Mac: `~/.microsoft/usersecrets/{project-id}/secrets.json`

### Option C: Environment Variables (Recommended for Production/CI-CD)

#### For MAUI:
Set environment variables before running the app:
```powershell
$env:CLOUDOSTAT_IoTHub__SharedAccessKey = "your-key"
```

#### For Blazor Web:
Set environment variables in your hosting environment:
```bash
export ASPNETCORE_IoTHub__SharedAccessKey="your-key"
```

Or in Azure App Service:
1. Go to Configuration
2. Add Application Setting: `IoTHub__SharedAccessKey` = `your-key`

### Option D: Azure Key Vault (Production Recommended)

For production deployments, store credentials in Azure Key Vault:

1. Create a Key Vault in Azure
2. Add secret: `IotHubSharedAccessKey`
3. Configure backend to use Key Vault:
   ```csharp
   builder.Configuration.AddAzureKeyVault(
       new Uri($"https://{keyVaultName}.vault.azure.net/"),
       new DefaultAzureCredential());
   ```

## Getting Your Shared Access Key

### From Azure Portal:

1. Navigate to your IoT Hub
2. Select **Device management** → **Devices**
3. Click your device (e.g., `cloudostat-meadow`)
4. Copy the **Primary Connection String**
5. Extract the **SharedAccessKey** part:
   ```
   HostName=usw-iot-cloudostat.azure-devices.net;
   DeviceId=cloudostat-meadow;
   SharedAccessKey=YOUR_KEY_HERE   ← This part
   ```

### Using Azure CLI:

```powershell
# Get the device connection string
az iot hub device-identity connection-string show `
  --hub-name usw-iot-cloudostat `
  --device-id cloudostat-meadow

# Output will be:
# HostName=usw-iot-cloudostat.azure-devices.net;DeviceId=cloudostat-meadow;SharedAccessKey=YOUR_KEY_HERE
```

## Configuration Priority (Highest to Lowest)

### MAUI:
1. Environment variables (`CLOUDOSTAT_*`)
2. `appsettings.Debug.json` (Debug build) or `appsettings.Release.json` (Release build)
3. `appsettings.json` (default)

### Blazor Web:
1. Azure Key Vault (if configured)
2. Environment variables (`ASPNETCORE_*`)
3. User Secrets (development only)
4. `appsettings.{Environment}.json` (e.g., `appsettings.Development.json`)
5. `appsettings.json` (default)

## Configuration Access in Code

### MAUI:
```csharp
public class DeviceControlService : IDeviceControlService
{
    public DeviceControlService(IConfiguration configuration)
    {
        _iotHubName = configuration["IoTHub:HubName"];
        _deviceId = configuration["IoTHub:DeviceId"];
        _sharedAccessKey = configuration["IoTHub:SharedAccessKey"];
    }
}
```

### Blazor Web:
```csharp
public class DeviceController : ControllerBase
{
    private readonly IConfiguration _configuration;
    
    public DeviceController(ILogger<DeviceController> logger, IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    [HttpGet("status")]
    public async Task<ActionResult> GetStatus()
    {
        var hubName = _configuration["IoTHub:HubName"];
        var key = _configuration["IoTHub:SharedAccessKey"];
        // ... use configuration
    }
}
```

## Troubleshooting Configuration Issues

### Issue: "Unauthorized" or "Forbidden" errors

**Cause:** Wrong or missing SharedAccessKey

**Solution:**
1. Verify the key is copied exactly (including special characters)
2. Ensure there are no leading/trailing spaces
3. Check that the key hasn't expired
4. Regenerate the device key in Azure Portal if needed

### Issue: Configuration not loading

**MAUI:**
1. Verify `appsettings.json` is in the project root
2. Check that `.csproj` includes it as an asset:
   ```xml
   <MauiAsset Include="appsettings.json" LogicalName="appsettings.json" />
   ```
3. Rebuild and check `bin` directory for the file

**Blazor Web:**
1. Verify `appsettings.json` and `appsettings.Development.json` exist
2. Check that User Secrets is initialized (if using)
3. Restart the development server

### Issue: Environment-specific config not used

**MAUI:**
- Ensure you're running the correct build configuration (Debug/Release)
- Check that debug/release file name matches exactly

**Blazor Web:**
- Verify `ASPNETCORE_ENVIRONMENT` is set correctly
- Development server defaults to "Development"
- Check `Properties/launchSettings.json`

## Security Best Practices

✅ **DO:**
- Use User Secrets for local development
- Use Azure Key Vault for production
- Use environment variables in CI/CD pipelines
- Never commit actual keys to the repository
- Rotate keys regularly
- Use minimal permission scopes

❌ **DON'T:**
- Hardcode credentials in source code
- Commit `appsettings.json` with real keys
- Share keys via email or chat
- Use the same key across environments
- Leave `SharedAccessKey` empty in production

## Verification

### Test MAUI Configuration:
```csharp
// In MauiApp or a debug page
var config = serviceProvider.GetService<IConfiguration>();
var hubName = config["IoTHub:HubName"];
System.Diagnostics.Debug.WriteLine($"Hub: {hubName}");
```

### Test Blazor Web Configuration:
```powershell
# Check local config is loaded correctly
curl http://localhost:5000/api/device/status

# Should return device status or proper error (not config error)
```

### Test WASM Client:
```javascript
// In browser console
fetch('/api/device/status').then(r => r.json()).then(console.log)
```

## Deployment Checklist

### Before deploying to production:

- [ ] Credentials removed from all source files
- [ ] User Secrets initialized locally (for local testing)
- [ ] Environment variables configured in hosting environment
- [ ] Azure Key Vault keys configured (if using Key Vault)
- [ ] Connection to IoT Hub tested
- [ ] Device authenticated successfully
- [ ] Reported properties received from device
- [ ] Desired properties sent to device successfully
- [ ] All build configurations pass
- [ ] Documentation updated with environment-specific values

## References

- [Microsoft Configuration Documentation](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [Azure IoT Hub Device Connection Strings](https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-connection-string)
- [MAUI Configuration Guide](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/appmodel/app-settings)
- [Safe Storage of Secrets in Development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
