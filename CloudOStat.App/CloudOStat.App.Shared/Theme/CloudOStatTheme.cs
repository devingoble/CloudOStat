using MudBlazor;

namespace CloudOStat.App.Shared.Theme;

public static class CloudOStatTheme
{
    public static MudTheme SmokerEmber { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = SmokerAppColorSchemes.SmokerEmber.Primary,
            Secondary = SmokerAppColorSchemes.SmokerEmber.Secondary,
            Tertiary = SmokerAppColorSchemes.SmokerEmber.Accent,
            Info = SmokerAppColorSchemes.SmokerEmber.Cooling,
            Success = SmokerAppColorSchemes.SmokerEmber.OnTemp,
            Warning = SmokerAppColorSchemes.SmokerEmber.Warning,
            Error = SmokerAppColorSchemes.SmokerEmber.Error,
            Dark = SmokerAppColorSchemes.SmokerEmber.Secondary,
            TextPrimary = SmokerAppColorSchemes.SmokerEmber.Secondary,
            TextSecondary = "#6B4A3A",
            TextDisabled = "#A89588",
            ActionDefault = SmokerAppColorSchemes.SmokerEmber.Secondary,
            ActionDisabled = "#BDB2A7",
            ActionDisabledBackground = "#EDE6DE",
            Background = SmokerAppColorSchemes.SmokerEmber.Background,
            Surface = "#FFFFFF",
            DrawerBackground = SmokerAppColorSchemes.SmokerEmber.Secondary,
            DrawerText = SmokerAppColorSchemes.SmokerEmber.Background,
            AppbarBackground = SmokerAppColorSchemes.SmokerEmber.Primary,
            AppbarText = SmokerAppColorSchemes.SmokerEmber.Background,
            LinesDefault = "#E0D6CC",
            LinesInputs = "#D4C6B8",
            TableLines = "#E0D6CC",
            TableStriped = "#F4EEE6",
            Divider = "#E0D6CC",
            DividerLight = "#EFE7DE",
            OverlayDark = "rgba(33, 20, 14, 0.5)",
            OverlayLight = "rgba(247, 243, 238, 0.5)"
        },
        PaletteDark = new PaletteDark
        {
            Primary = SmokerAppColorSchemes.SmokerEmber.Accent,
            Secondary = SmokerAppColorSchemes.SmokerEmber.Primary,
            Tertiary = SmokerAppColorSchemes.SmokerEmber.Secondary,
            Info = SmokerAppColorSchemes.SmokerEmber.Cooling,
            Success = SmokerAppColorSchemes.SmokerEmber.OnTemp,
            Warning = SmokerAppColorSchemes.SmokerEmber.Warning,
            Error = SmokerAppColorSchemes.SmokerEmber.Error,
            Dark = "#120B08",
            TextPrimary = SmokerAppColorSchemes.SmokerEmber.Background,
            TextSecondary = "#C9BFB5",
            TextDisabled = "#9B8E82",
            ActionDefault = SmokerAppColorSchemes.SmokerEmber.Accent,
            ActionDisabled = "#6B5C52",
            ActionDisabledBackground = "#3A2A23",
            Background = "#1B120E",
            Surface = "#2A1C16",
            DrawerBackground = "#1F140F",
            DrawerText = SmokerAppColorSchemes.SmokerEmber.Background,
            AppbarBackground = "#241712",
            AppbarText = SmokerAppColorSchemes.SmokerEmber.Background,
            LinesDefault = "#3A2A23",
            LinesInputs = "#4A352D",
            TableLines = "#3A2A23",
            TableStriped = "#241712",
            Divider = "#3A2A23",
            DividerLight = "#4A352D",
            OverlayDark = "rgba(0, 0, 0, 0.6)",
            OverlayLight = "rgba(255, 255, 255, 0.08)"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "0.875rem",
                FontWeight = "400",
                LineHeight = "1.5",
                LetterSpacing = "0.00938em"
            },
            H1 = new H1Typography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "2.5rem",
                FontWeight = "600",
                LineHeight = "1.2",
                LetterSpacing = "-0.01562em"
            },
            H2 = new H2Typography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "2rem",
                FontWeight = "600",
                LineHeight = "1.25",
                LetterSpacing = "-0.00833em"
            },
            H3 = new H3Typography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "1.75rem",
                FontWeight = "600",
                LineHeight = "1.3",
                LetterSpacing = "0em"
            },
            H4 = new H4Typography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "1.5rem",
                FontWeight = "600",
                LineHeight = "1.35",
                LetterSpacing = "0.00735em"
            },
            H5 = new H5Typography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "1.25rem",
                FontWeight = "600",
                LineHeight = "1.4",
                LetterSpacing = "0em"
            },
            H6 = new H6Typography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "1.125rem",
                FontWeight = "600",
                LineHeight = "1.45",
                LetterSpacing = "0.0075em"
            },
            Subtitle1 = new Subtitle1Typography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "1rem",
                FontWeight = "500",
                LineHeight = "1.5",
                LetterSpacing = "0.00938em"
            },
            Subtitle2 = new Subtitle2Typography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "0.875rem",
                FontWeight = "500",
                LineHeight = "1.43",
                LetterSpacing = "0.00714em"
            },
            Body1 = new Body1Typography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "1rem",
                FontWeight = "400",
                LineHeight = "1.5",
                LetterSpacing = "0.00938em"
            },
            Body2 = new Body2Typography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "0.875rem",
                FontWeight = "400",
                LineHeight = "1.43",
                LetterSpacing = "0.01071em"
            },
            Button = new ButtonTypography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "0.875rem",
                FontWeight = "600",
                LineHeight = "1.75",
                LetterSpacing = "0.02857em"
            },
            Caption = new CaptionTypography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "0.75rem",
                FontWeight = "400",
                LineHeight = "1.66",
                LetterSpacing = "0.03333em"
            },
            Overline = new OverlineTypography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "0.75rem",
                FontWeight = "600",
                LineHeight = "2.66",
                LetterSpacing = "0.08333em"
            }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "6px",
            DrawerWidthLeft = "260px",
            AppbarHeight = "56px"
        },
        ZIndex = new ZIndex()
    };
}
