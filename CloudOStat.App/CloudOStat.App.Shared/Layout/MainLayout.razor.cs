using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CloudOStat.App.Shared.Layout;

public class MainLayoutBase : LayoutComponentBase
{
    protected bool _drawerOpen = true;
    protected DrawerVariant _drawerVariant = DrawerVariant.Persistent;
    protected bool _isMobile;
    protected bool _isDesktop;

    protected void HandleBreakpointChanged(Breakpoint breakpoint)
    {
        UpdateLayout(breakpoint);
        StateHasChanged();
    }

    protected void ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
        UpdateDrawerVariant();
    }

    private void UpdateLayout(Breakpoint breakpoint)
    {
        _isMobile = breakpoint < Breakpoint.Md;
        _isDesktop = breakpoint >= Breakpoint.Lg;

        if (_isMobile)
        {
            _drawerVariant = DrawerVariant.Temporary;
            _drawerOpen = false;
            return;
        }

        if (_isDesktop)
        {
            _drawerVariant = DrawerVariant.Persistent;
            _drawerOpen = true;
            return;
        }

        UpdateDrawerVariant();
    }

    private void UpdateDrawerVariant()
    {
        if (_isMobile)
        {
            _drawerVariant = DrawerVariant.Temporary;
            return;
        }

        _drawerVariant = _isDesktop
            ? DrawerVariant.Persistent
            : _drawerOpen ? DrawerVariant.Temporary : DrawerVariant.Mini;
    }
}
