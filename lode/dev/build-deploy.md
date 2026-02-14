# Build, Deploy & Development

## Development Setup

### Prerequisites
- **.NET 10 SDK** (locked via `global.json`)
- **Visual Studio 2026** (Community or higher)
- **Meadow CLI** (if developing hardware locally)

### Local Development (AppHost)

**AppHost** (`CloudOStat.AppHost`) orchestrates multi-project local dev:
```bash
cd CloudOStat.AppHost
dotnet run
```

This launches:
1. Blazor Server (`CloudOStat.App.Web`) on https://localhost:17128
2. MAUI app window (if on Windows)
3. Aspire Dashboard on https://localhost:21052
4. Telemetry aggregation (OpenTelemetry)

**Environment Variables** (launchSettings.json):
```json
{
  "ASPNETCORE_ENVIRONMENT": "Development",
  "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:21052"
}
```

## Build Process

### Single Project
```bash
dotnet build CloudOStat.App/CloudOStat.App.Web/
```

### Entire Solution
```bash
dotnet build
```

**Output**:
- MAUI: `.app`, `.exe` (platform-specific)
- Blazor: `.dll` + static assets
- Common/Device: `.dll`

### Publish

**MAUI Desktop**:
```bash
dotnet publish -c Release -f net10.0-windows
```

**Blazor Server**:
```bash
dotnet publish -c Release -o ./publish
```

**Docker** (future):
- Dockerfile in `CloudOStat.App.Web`
- Publish as container image

## Testing

Not yet structured; future:
- Unit tests: `CloudOStat.*.Tests` projects
- Integration tests: AppHost-based full-stack tests
- UI tests: Playwright (Blazor components)

## Package Management

### Add a New Package
1. Add to `Directory.Packages.props`:
   ```xml
   <PackageVersion Include="PackageName" Version="x.y.z" />
   ```
2. Reference in `.csproj` without version:
   ```xml
   <PackageReference Include="PackageName" />
   ```
3. `dotnet restore` or rebuild

### Update Packages
```bash
dotnet list package --outdated
```

Manual update in `Directory.Packages.props`, then rebuild.

## CI/CD (GitHub Actions)

**Planned** (not yet configured):
- Trigger: Push to main, PR
- Jobs:
  - Build solution
  - Run tests
  - Publish MAUI + Blazor artifacts
  - Deploy web to hosting (Vercel, Azure, etc.)

## .NET 10 Specific Notes

- **MAUI 10.0.40**: Latest stable
- **Blazor 10.0.3**: Interactive Server + WASM modes
- **C# 14** language features enabled
- **`<Nullable>enable</Nullable>`**: Null-safe by default
- **File-scoped namespaces**: Standard in new `.cs` files

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| Build fails: `IFormFactor` not found | Service not registered | Check `MauiProgram.cs` or `Program.cs` has `.AddSingleton<IFormFactor, FormFactor>()` |
| MAUI window won't open | Missing platform implementation | Verify `Platforms/Windows/App.xaml` exists |
| Blazor WASM blank page | JS interop missing | Check `_Imports.razor` imports; rebuild WASM |
| Telemetry not appearing | AppHost not running | Start AppHost first, then access dashboard |

---

See [practices.md](../practices.md) for DI details; [lode-map.md](../lode-map.md) for project structure.
