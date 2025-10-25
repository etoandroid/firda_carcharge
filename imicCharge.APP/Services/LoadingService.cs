using imicCharge.APP.Controls;

namespace imicCharge.APP.Services;

public static class LoadingService
{
    private static LoadingIndicator? _loadingIndicator;

    public static void Register(LoadingIndicator loadingIndicator)
    {
        _loadingIndicator = loadingIndicator;
    }

    // Use InvokeOnMainThreadAsync for UI updates from any thread
    public static Task ShowAsync(string message = "Loading...")
    {
        if (_loadingIndicator == null)
            return Task.CompletedTask;

        return MainThread.InvokeOnMainThreadAsync(() => _loadingIndicator.Show(message));
    }

    public static Task HideAsync()
    {
        if (_loadingIndicator == null)
            return Task.CompletedTask;

        return MainThread.InvokeOnMainThreadAsync(() => _loadingIndicator.Hide());
    }
}