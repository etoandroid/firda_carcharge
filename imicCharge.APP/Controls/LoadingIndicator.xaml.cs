namespace imicCharge.APP.Controls;

public partial class LoadingIndicator : ContentView
{
    public LoadingIndicator()
    {
        InitializeComponent();
        Opacity = 0;
    }

    public async Task Show(string message = "Lastar inn..")
    {
        MessageLabel.Text = message;
        Spinner.IsRunning = true;
        IsVisible = true;
        InputTransparent = false; // Block input when visible
        await this.FadeTo(1, 250, Easing.CubicOut); // Fade in
    }

    public async Task Hide()
    {
        if (!IsVisible)
            return;
        
        await this.FadeTo(0, 250, Easing.CubicIn);
        Spinner.IsRunning = false;
        IsVisible = false;
        InputTransparent = true; // Allow input when hidden
    }
}