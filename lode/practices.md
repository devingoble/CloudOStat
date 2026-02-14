# CloudOStat Architecture & Practices

## Solution Multi-Targeting Strategy
- **MAUI** (`CloudOStat.App.csproj` in `CloudOStat.App/`) – targets `net10.0`, hosts blazor webview.
- **Blazor Web Server** (`CloudOStat.App.Web`) – targets `net10.0`, renders Shared components interactively.
- **Blazor WebAssembly Client** (`CloudOStat.App.Web.Client`) – targets `net10.0`, browser-side rendering.
- **Common/Hardware** (`CloudOStat.Common`) – targets `netstandard2.1` for broad compatibility.
- **Device/Meadow** (`CloudOStat.Meadow`, `CloudOStat.LocalHardware`) – targets `net10.0`, hardware control.
- **AppHost** (`CloudOStat.AppHost`) – .NET Aspire orchestrator, local dev multi-project runner.

## Shared Component Model
All UI and shared logic lives in **`CloudOStat.App.Shared`** (`net10.0` class library):
- Razor components (`.razor` files) + code-behind (`.razor.cs`)
- Pages folder: `Dashboard.razor`, `Settings.razor`, `About.razor`, `Counter`, `Weather` (demos)
- Layout: `MainLayout.razor` (responsive, theme-aware) + `NavMenu.razor`, `BottomNav.razor`
- Services: `NavigationService` (route definitions, navigation API)
- Interfaces: `IFormFactor` (platform-specific form factor detection)
- Theme: `Themes.cs` (four color schemes), `CloudOStatTheme.cs` (MudBlazor theme definition)
- Styles: `MainLayout.razor.css`, `BottomNav.razor.css`, `app.css` (scoped + global CSS)

## Dependency Injection Pattern
Each host registers shared services independently:

**MAUI (MauiProgram.cs)**:
```csharp
builder.Services.AddMudServices();
builder.Services.AddSingleton<NavigationService>();
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddMauiBlazorWebView();
```

**Blazor Web Server (Program.cs)**:
```csharp
builder.Services.AddMudServices();
builder.Services.AddSingleton<NavigationService>();
builder.Services.AddSingleton<IFormFactor, FormFactor>();
```

**Blazor WASM Client (Program.cs)**:
- Mirrors server registration where applicable.

## Package Management
**Central versioning via `Directory.Packages.props`** (centralized package version management):
- .NET / MAUI / Blazor: 10.0.x (latest .NET 10)
- MudBlazor: 8.15.0
- Meadow: 2.5.0
- OpenTelemetry: 1.15.0 (observability/tracing)
- Resilience: 10.3.0 (HTTP retry, circuit breaking)

Update patterns:
1. Add new packages to `Directory.Packages.props`
2. Reference in `.csproj` without version (version inherited)
3. Use `dotnet list package --outdated` to audit

## Hardware Abstraction
**`CloudOStat.Common` (netstandard2.1)**:
- `IHardwarePackage`: Interface for three sensors, relay, display, LED
- `HardwarePackage`: Meadow F7 implementation with MAX31855 thermocouple amplifiers
- `IMAX31855` / `MAX31855`: Thermocouple driver

Hosts (MAUI, Web, Device) instantiate and inject via DI as needed.

## Theming Strategy
**Color Schemes** (`Themes.cs`):
- SmokerEmber (default): warm, earthy tones (primary #B33F1C)
- BackyardPitmaster: steel grays (primary #2F3E46)
- TemperatureGradient: warm-to-cool spectrum (primary #F4A261)
- FarmhouseModern: sage + clay (primary #6B705C)

Each scheme defines:
- Primary, Secondary, Accent, Background
- Heating (active red-orange), Cooling (blue-grey), OnTemp (green), Warning (amber), Error (deep red)

**Theme Binding** (`CloudOStatTheme.cs`):
- Single MudTheme instance (`SmokerEmber`)
- Light + Dark palettes aligned to color scheme
- Applied in `App.razor` via MudTheme provider

**CSS Structure**:
- Component-scoped: `MainLayout.razor.css`, `BottomNav.razor.css`
- Global shared: `app.css`
- MAUI platform-specific: CSS in platform folders if needed

## Navigation Service
**Route Definitions** (`NavigationService.cs`):
- `PrimaryNav`: Home (MatchAll=true), Dashboard, Settings
- `SecondaryNav`: Counter, Weather, About
- `Navigate(route)` method wraps `NavigationManager.NavigateTo(route)`

Used by:
- `NavMenu.razor`: Sidebar with primary/secondary nav
- `BottomNav.razor`: Bottom tab bar (mobile-friendly)
- Layout: Responsive switching between menu/bottom-nav

## Observability & Resilience
- **ServiceDefaults** project provides:
  - HTTP resilience (retry, circuit breaker)
  - Service discovery helpers
  - OpenTelemetry hooks for tracing
- **AppHost** (Aspire):
  - Orchestrates local dev environment
  - Logs + traces aggregated in dashboard

## Cross-Platform Responsive Design
- `MainLayout` detects platform via `IFormFactor`
- MAUI/Web/WASM adapters provide form factor (phone, tablet, desktop)
- CSS breakpoints and MudBlazor responsive classes handle layout
- BottomNav shown on mobile; NavMenu on desktop (responsive toggle in MainLayout.razor.cs)
