namespace imicCharge.APP;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new LoginPage());

        // Platform-specific window configuration.
        window.Created += (s, e) =>
        {
            // This code will only compile and run on the Windows platform.
#if WINDOWS
                const int windowWidth = 600;
                const int windowHeight = 850;

                // Get the native window object
                var nativeWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (nativeWindow == null) return;

                var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                // Resize and lock the window
                if (appWindow != null)
                {
                    appWindow.Resize(new Windows.Graphics.SizeInt32(windowWidth, windowHeight));
                    
                    if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
                    {
                        p.IsResizable = false;
                        p.IsMaximizable = false; // Also disable the maximize button
                    }
                }
#endif
        };

        return window;
    }
}
