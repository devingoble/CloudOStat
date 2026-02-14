namespace CloudOStat.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage()) { Title = "CloudOStat.App" };

#if WINDOWS
        window.Width = 1200;
        window.Height = 800;
        window.MinimumWidth = 900;
        window.MinimumHeight = 600;
#endif

        return window;
    }
}
