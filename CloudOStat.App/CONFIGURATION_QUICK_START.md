# Configuration Setup Checklist

## Quick Start - 5 Minutes

### 1. Get Your Shared Access Key
```powershell
# Run this in PowerShell to get your device key
az iot hub device-identity connection-string show `
  --hub-name usw-iot-cloudostat `
  --device-id cloudostat-meadow
```
Extract the `SharedAccessKey=...` part from the output.

### 2. For MAUI App (Native)
- [ ] Open `CloudOStat.App/CloudOStat.App/appsettings.json`
- [ ] Replace empty `SharedAccessKey` with your key
- [ ] Rebuild the app

Or use environment variable:
```powershell
$env:CLOUDOSTAT_IoTHub__SharedAccessKey = "your-key-here"
```

### 3. For Blazor Web Backend
**Option A - User Secrets (Recommended for Local Dev):**
```powershell
cd CloudOStat.App/CloudOStat.App.Web
dotnet user-secrets init
dotnet user-secrets set "IoTHub:SharedAccessKey" "your-key-here"
```

**Option B - Direct File:**
- [ ] Open `CloudOStat.App/CloudOStat.App.Web/appsettings.Development.json`
- [ ] Replace empty `SharedAccessKey` with your key

### 4. For Blazor WASM Client
- [ ] No configuration needed - uses backend API

## Files to Create (If Not Present)

Create these optional files for environment-specific overrides:

### MAUI
- `CloudOStat.App/CloudOStat.App/appsettings.Debug.json` (debug overrides)
- `CloudOStat.App/CloudOStat.App/appsettings.Release.json` (release overrides)

Template:
```json
{
  "IoTHub": {
    "SharedAccessKey": "your-key-here"
  }
}
```

## Verification

### Test MAUI:
1. Run the MAUI app
2. Navigate to "Device Control" page
3. Should load device status without errors

### Test Blazor Web:
1. Run the backend: `dotnet run` in `CloudOStat.App/CloudOStat.App.Web`
2. Open browser to `http://localhost:5000`
3. Navigate to "Device Control" page
4. Should load device status without errors

### Test WASM Client:
1. Run backend (required)
2. Navigate to WASM app in browser
3. Open "Device Control" page
4. Should load device status without errors

## Files Overview

| Project | File | Purpose | Required |
|---------|------|---------|----------|
| MAUI | `appsettings.json` | Base config | ✅ Yes |
| MAUI | `appsettings.Debug.json` | Dev overrides | ❌ Optional |
| MAUI | `appsettings.Release.json` | Prod overrides | ❌ Optional |
| Web | `appsettings.json` | Base config | ✅ Yes |
| Web | `appsettings.Development.json` | Dev overrides | ✅ Yes |
| WASM | None | Uses backend API | ✅ N/A |

## Security Notes

⚠️ **Important:** 
- Never commit actual `SharedAccessKey` values to the repository
- The repository only has empty placeholders
- Use User Secrets or environment variables for local development
- Use Azure Key Vault or managed identities for production

## Next Steps

1. ✅ **Get your key** (see Quick Start)
2. ✅ **Set up configuration** using your preferred method
3. ✅ **Test the connection** (see Verification)
4. ✅ **Deploy** with production credentials

For detailed information, see `CONFIGURATION_SETUP.md`
