using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace CloudOStat.App.Shared.Layout;

public class MainLayoutBase : LayoutComponentBase
{
    protected bool _drawerOpen = false;
    protected DrawerVariant _drawerVariant = DrawerVariant.Mini;
    protected bool _isMobile;
    protected bool _isDesktop;
    protected bool _showStackTrace;
    protected ErrorBoundary? _errorBoundary;

    [Inject]
    public IJSRuntime JS { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var savedState = await JS.InvokeAsync<string>("localStorage.getItem", "drawerOpen");
            if (savedState != null && bool.TryParse(savedState, out var isOpen))
            {
                _drawerOpen = isOpen;
            }
        }
        catch
        {
            // localStorage not available (e.g., private browsing), use default
        }
    }

    protected void HandleBreakpointChanged(Breakpoint breakpoint)
    {
        UpdateLayout(breakpoint);
        StateHasChanged();
    }

    protected async Task ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
        await SaveDrawerStateAsync();
    }

    protected void RecoverFromError()
    {
        _errorBoundary?.Recover();
        _showStackTrace = false;
    }

    private void UpdateLayout(Breakpoint breakpoint)
    {
        _isMobile = breakpoint < Breakpoint.Md;
        _isDesktop = breakpoint >= Breakpoint.Lg;

        if (_isMobile)
        {
            // Mobile: No drawer at all, handled in MainLayout.razor
            _drawerOpen = false;
            return;
        }

        // Tablet and Desktop: Use Mini variant
        // Open=false shows mini (icon bar), Open=true expands to full
        _drawerVariant = DrawerVariant.Mini;
        
        // Default to mini state (not expanded)
        if (_drawerOpen)
        {
            // Keep whatever state was saved, but ensure it's reasonable
            _drawerOpen = false;
        }
    }

    private async Task SaveDrawerStateAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("localStorage.setItem", "drawerOpen", _drawerOpen.ToString().ToLower());
        }
        catch
        {
            // localStorage not available, silently fail
        }
    }
}
