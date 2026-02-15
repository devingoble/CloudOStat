using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace CloudOStat.App.Shared.Services;

public record NavItem(string Label, string Icon, string Route, bool MatchAll = false);

public class NavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
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
    public void Navigate(string route)
    {
        try
        {
            var navigationManager = _serviceProvider.GetService<NavigationManager>();
            if (navigationManager != null)
            {
                navigationManager.NavigateTo(route);
            }
        }
        catch (Exception)
        {
            // Silently fail if NavigationManager is not available (e.g., in certain MAUI contexts)
        }
    }
}
