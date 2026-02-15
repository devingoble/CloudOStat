namespace CloudOStat.App;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        // Handle BlazorWebView initialization
        blazorWebView.BlazorWebViewInitialized += OnBlazorWebViewInitialized;
    }

    private void OnBlazorWebViewInitialized(object? sender, Microsoft.AspNetCore.Components.WebView.BlazorWebViewInitializedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("BlazorWebView initialized successfully");
    }
}
