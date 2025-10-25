using imicCharge.APP.Controls;

namespace imicCharge.APP.Services;

public static class PopupService
{
    private static CustomAlert? _customAlert;

    public static void Register(CustomAlert customAlert)
    {
        _customAlert = customAlert;
    }

    public static Task ShowAlertAsync(string title, string message, string buttonText = "OK")
    {
        if (_customAlert == null)
        {
            // Fails silently if the alert is not registered, preventing crashes.
            return Task.CompletedTask;
        }

        // Ensure the UI update is run on the main thread.
        return MainThread.InvokeOnMainThreadAsync(() =>
            _customAlert.Show(title, message, buttonText));
    }
}