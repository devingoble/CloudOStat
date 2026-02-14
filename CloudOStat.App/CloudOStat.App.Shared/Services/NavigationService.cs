using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CloudOStat.App.Shared.Services;

public record NavItem(string Label, string Icon, string Route, bool MatchAll = false);

public class NavigationService
{
    private readonly NavigationManager _navigationManager;

    public NavigationService(NavigationManager navigationManager)
    {
        ArgumentNullException.ThrowIfNull(navigationManager);
        _navigationManager = navigationManager;
    }

    public IReadOnlyList<NavItem> PrimaryNav { get; } =
    [
        new NavItem("Home", Icons.Material.Filled.Home, "/", true),
        new NavItem("Dashboard", Icons.Material.Filled.Dashboard, "/dashboard"),
        new NavItem("Settings", Icons.Material.Filled.Settings, "/settings")
    ];

    public IReadOnlyList<NavItem> SecondaryNav { get; } =
    [
        new NavItem("Counter", Icons.Material.Filled.Add, "/counter"),
        new NavItem("Weather", Icons.Material.Filled.List, "/weather"),
        new NavItem("About", Icons.Material.Filled.Info, "/about")
    ];

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    /// <param name="route">The target route.</param>
    public void Navigate(string route) => _navigationManager.NavigateTo(route);
}
