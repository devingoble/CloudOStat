# CloudOStat UI Architecture

## Component hierarchy, responsive design, theming, error handling, and platform abstraction

### Overview
CloudOStat uses a shared Razor component library (`CloudOStat.App.Shared`) consumed by both MAUI (`CloudOStat.App`) and Blazor WebAssembly/Server (`CloudOStat.App.Web` + `.Web.Client`). This enables write-once UI with platform-specific hosting.

---

## Component Structure

### Layout Components
- **MainLayout.razor/cs**: Root layout with responsive drawer, app bar, and error boundary
  - Uses MudBlazor's `MudLayout`, `MudAppBar`, `MudDrawer`, `MudMainContent`
  - Toggles between desktop (persistent drawer) and mobile (temporary drawer + bottom nav)
  - Wraps entire layout in `ErrorBoundary` for Blazor rendering error recovery
- **NavMenu.razor**: Navigation list for drawer
- **BottomNav.razor**: Mobile-only bottom navigation bar (hidden on desktop/tablet)

### Pages
- **Dashboard.razor**: Main dashboard view
- **Settings.razor**: Application settings
- **About.razor**: About page

### Responsive Behavior
Handled via MudBlazor's `MudBreakpointProvider`:
- **Mobile** (`< Breakpoint.Md`): No drawer, bottom nav visible, no hamburger menu
- **Tablet/Desktop** (`>= Breakpoint.Md`): Mini drawer (icon bar) by default, hamburger expands to full width, no bottom nav

#### Drawer Behavior by Breakpoint

**Mobile (< Breakpoint.Md):**
- Drawer is completely hidden (not rendered)
- Hamburger button is hidden
- BottomNav provides navigation
- `_isMobile = true`, `_drawerOpen = false`

**Tablet/Desktop (≥ Breakpoint.Md):**
- Drawer uses `DrawerVariant.Mini`
- Always visible in mini state (icon bar) by default
- Hamburger button toggles between mini and expanded states
- `_drawerOpen = false` → Mini state (icons only)
- `_drawerOpen = true` → Expanded state (full width with labels)
- No bottom navigation

#### Implementation Details

**MainLayout.razor.cs:**
```csharp
// Default state: Mini drawer for tablet/desktop
protected DrawerVariant _drawerVariant = DrawerVariant.Mini;

private void UpdateLayout(Breakpoint breakpoint)
{
    _isMobile = breakpoint < Breakpoint.Md;
    _isDesktop = breakpoint >= Breakpoint.Lg;

    if (_isMobile)
    {
        // Mobile: No drawer rendered
        _drawerOpen = false;
        return;
    }

    // Tablet/Desktop: Mini variant
    // Open=false shows mini bar, Open=true expands full width
    _drawerVariant = DrawerVariant.Mini;
}
```

**MainLayout.razor:**
```razor
<MudAppBar Color="Color.Primary" Elevation="1">
    @if (!_isMobile)
    {
        <MudIconButton Icon="@Icons.Material.Filled.Menu" ... />
    }
    <MudText Typo="Typo.h6">CloudOStat</MudText>
</MudAppBar>

@if (!_isMobile)
{
    <MudDrawer @bind-Open="_drawerOpen"
               Variant="_drawerVariant"
               ClipMode="DrawerClipMode.Always">
        <NavMenu />
    </MudDrawer>
}

@if (_isMobile)
{
    <BottomNav />
}
```

**Key Design Decision:**
- `ClipMode="DrawerClipMode.Always"` ensures drawer clips under AppBar and content adjusts properly
- Mini variant with `Open` property provides seamless toggle between mini/expanded without layout shifts
- Conditional rendering (`@if (!_isMobile)`) prevents drawer from being in DOM on mobile devices

---

## Error Handling

### Three-Layer Strategy

#### 1. MAUI Global Exception Handlers (App.xaml.cs)
Catches unhandled exceptions at the application level:
- `AppDomain.CurrentDomain.UnhandledException` – Synchronous unhandled exceptions
- `TaskScheduler.UnobservedTaskException` – Async exceptions that escape try/catch

**Behavior:**
- Logs exception details to debug output
- Displays user-friendly alert dialog with error message
- Allows app to continue running when possible

```csharp
private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    if (e.ExceptionObject is Exception ex)
    {
        LogException(ex, "Unhandled Exception");
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await ShowErrorDialogAsync(ex);
        });
    }
}
```

#### 2. Blazor ErrorBoundary Component (MainLayout.razor)
Catches component rendering errors within Blazor:
- Wraps the entire `MudLayout` in `<ErrorBoundary>`
- Provides custom `<ErrorContent>` UI with:
  - User-friendly error message
  - "Try Again" button to recover without reload
  - "Show Details" toggle for stack trace
  - Styled error panel using MudBlazor components

**Recovery:**
- User clicks "Try Again" → calls `_errorBoundary?.Recover()` → re-renders component tree
- Falls back to standard Blazor error UI if boundary fails

#### 3. Debug Logging (MauiProgram.cs)
Enhanced logging configuration:
- Debug builds: `LogLevel.Debug` with `AddDebug()` and developer tools
- Release builds: `LogLevel.Warning` to reduce noise
- Filtered logging for noisy Blazor internals

---

## MudBlazor Static Assets in MAUI

### Critical Configuration
MAUI projects require explicit MudBlazor static asset references in `wwwroot/index.html`:

```html
<!-- Required MudBlazor assets -->
<link rel="stylesheet" href="_content/MudBlazor/MudBlazor.min.css" />
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

### Common Issue: mudElementRef.getBoundingClientRect undefined
**Symptom:** `JSException: Could not find 'mudElementRef.getBoundingClientRect' ('mudElementRef' was undefined)`

**Root Cause:** Missing `MudBlazor.min.js` in MAUI's `index.html`. Without this script, MudBlazor's JavaScript interop library never loads, causing `mudElementRef` to be undefined when components try to call JavaScript methods.

**Solution:** Add both MudBlazor CSS and JS references to `CloudOStat.App/wwwroot/index.html`:
- `MudBlazor.min.css` – Required for styling
- `MudBlazor.min.js` – Required for JSInterop functionality (getBoundingClientRect, resize observers, etc.)

**Affected Components:**
- `MudDrawer` – Height calculations and clip mode
- `MudNavGroup` – Collapse/expand animations  
- `MudCollapse` – Animated height transitions
- `MudBreakpointProvider` – Resize event handling

**Note:** The web project (`App.razor`) includes these by default, but MAUI projects must add them manually.

---

## Theming

### MudBlazor Theme System
- **CloudOStatTheme.cs**: Defines custom theme (SmokerEmber) with typography, palette, shadows
- **MudThemeProvider**: Applied in MainLayout.razor
- **CSS files**:
  - `app.css` (global): Site-wide styles, mobile safe-area adjustments
  - `MainLayout.razor.css`, `BottomNav.razor.css` (scoped): Component-specific styles

### Styling Guidelines
**Prefer MudBlazor's theming system over custom CSS:**
- Use `CloudOStatTheme.cs` to define colors, typography, spacing, shadows, and component defaults
- Rely on MudBlazor's built-in component styling and utility classes (`mud-px-4`, `mud-mt-2`, etc.)
- Only create custom CSS when:
  - MudBlazor explicitly recommends it (e.g., scoped styles for layout-specific adjustments)
  - Platform-specific requirements (e.g., mobile safe-area handling)
  - Global styles not covered by MudBlazor's theme system

**Rationale:** MudBlazor's theme engine ensures consistent styling, automatic dark mode support, and easier maintenance. Custom CSS can conflict with theme updates and requires manual dark mode handling.

### Overriding MudBlazor Component Styles
**Important: Use global CSS for MudBlazor overrides, not scoped CSS**

**Problem:** MudBlazor components (like `MudNavLink`) use hardcoded defaults that don't respect theme palette properties. For example, `MudNavLink` icons default to `#616161` (Material Design gray) regardless of `DrawerText` color in the theme.

**Solution:** Override in global CSS (`app.css`), not component-scoped `.razor.css` files.

**Why scoped CSS fails:**
- Blazor's CSS isolation with `::deep` is unreliable with MudBlazor components
- MudBlazor's internal DOM structure and CSS specificity can prevent scoped styles from applying
- Global CSS is the officially recommended approach for MudBlazor style overrides

**Example - NavMenu icon colors:**
```css
/* In app.css (global) */
.mud-nav-link .mud-nav-link-icon-default,
.mud-nav-link .mud-icon-root {
    color: #F7F3EE !important;  /* theme DrawerText color */
}

.mud-nav-link.active .mud-icon-root {
    color: #B33F1C !important;  /* theme Primary color */
}
```

**When to use `!important`:**
- Required when overriding MudBlazor's inline or high-specificity styles
- Use sparingly; only when necessary to override component defaults

### Color Scheme (SmokerEmber)
- Primary: Ember orange (#B33F1C)
- Secondary: Charcoal gray (#5A2E1A)
- Accent: Glazed Honey (#F2A65A)
- Background: Ash White (#F7F3EE)

---

## Platform Abstraction

### IFormFactor Service
- **Interface**: `IFormFactor` in `CloudOStat.App.Shared/Services`
- **Implementations**:
  - MAUI: `FormFactor` in `CloudOStat.App/Services`
  - Web: TBD (server/client-specific implementations)

**Purpose**: Detect device capabilities, screen size, input methods
**Usage**: Injected into components needing platform-specific behavior

### Registration
Each host registers its own implementation:
- `MauiProgram.cs`: `builder.Services.AddSingleton<IFormFactor, FormFactor>()`
- `Program.cs` (Web): Similar registration for web-specific version

---

## Navigation

### NavigationService
- Centralized route definitions in `Services/NavigationService.cs`
- Maps display names to route paths
- Used by NavMenu and BottomNav for consistent navigation
- Example: `Dashboard → /`, `Settings → /settings`, `About → /about`

---

## Best Practices

### Adding New Pages
1. Create `.razor` file in `CloudOStat.App.Shared/Pages`
2. Add route definition to `NavigationService`
3. Add navigation entry to `NavMenu.razor` and/or `BottomNav.razor` (if mobile-relevant)
4. Wrap sensitive operations in try/catch or use ErrorBoundary child boundaries for isolation

### Handling Async Operations
- Always pass `CancellationToken` to async methods
- Wrap async initialization in `OnInitializedAsync` with error handling
- Use `InvokeAsync(() => StateHasChanged())` when updating from background threads

### MAUI Static Assets Checklist
When adding UI libraries to MAUI projects:
1. Verify CSS references in `wwwroot/index.html`
2. Verify JavaScript references in `wwwroot/index.html`
3. Test in MAUI app first (web projects usually include assets automatically)
4. Check browser console for 404 errors on static assets

---

## Related Files
- MainLayout: `CloudOStat.App.Shared/Layout/MainLayout.razor`, `.razor.cs`, `.razor.css`
- Error handling: `CloudOStat.App/App.xaml.cs`, `CloudOStat.App/MauiProgram.cs`
- Theme: `CloudOStat.App.Shared/Theme/CloudOStatTheme.cs`
- Navigation: `CloudOStat.App.Shared/Services/NavigationService.cs`
- MAUI host page: `CloudOStat.App/wwwroot/index.html`
- Web host page: `CloudOStat.App.Web/Components/App.razor`
