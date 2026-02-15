namespace CloudOStat.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Catch unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
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

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException(e.Exception, "Unobserved Task Exception");
        e.SetObserved();
        
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await ShowErrorDialogAsync(e.Exception);
        });
    }

    private void LogException(Exception ex, string source)
    {
        System.Diagnostics.Debug.WriteLine($"[{source}] {ex.GetType().Name}: {ex.Message}");
        System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
        
        if (ex.InnerException != null)
        {
            System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
        }
    }

    private async Task ShowErrorDialogAsync(Exception ex)
    {
        try
        {
            var page = Current?.Windows[0]?.Page;
            if (page != null)
            {
                await page.DisplayAlertAsync(
                    "Application Error",
                    $"An unexpected error occurred:\n\n{ex.Message}\n\nThe application will continue running, but some features may not work correctly.",
                    "OK");
            }
        }
        catch
        {
            // If we can't show the dialog, just log it
            System.Diagnostics.Debug.WriteLine("Failed to show error dialog");
        }
    }
}
