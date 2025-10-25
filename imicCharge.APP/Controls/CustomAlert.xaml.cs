namespace imicCharge.APP.Controls;

public partial class CustomAlert : ContentView
{
    private TaskCompletionSource<bool>? _tcs;

    public CustomAlert()
    {
        InitializeComponent();
        Opacity = 0;
    }

    public async Task Show(string title, string message, string buttonText)
    {
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        OkButton.Text = buttonText;

        _tcs = new TaskCompletionSource<bool>();

        IsVisible = true;
        InputTransparent = false; // Block input when visible
        await this.FadeTo(1, 250, Easing.CubicInOut); // Fade in over 250ms
        await _tcs.Task; // Wait for button click
        await Hide();
    }

    private async Task Hide()
    {
        await this.FadeTo(0, 250, Easing.CubicIn); // Fade out over 250ms
        IsVisible = false;
        InputTransparent = true;
    }

    private void OnOkButtonClicked(object sender, EventArgs e)
    {
        _tcs?.SetResult(true);
    }
}