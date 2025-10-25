using imicCharge.APP.Services;
using System.Timers;

namespace imicCharge.APP;

[QueryProperty(nameof(ChargerId), "ChargerId")]
public partial class ChargingPage : ContentPage
{
    public string? ChargerId { get; set; }
    private readonly ApiService _apiService;
    private System.Timers.Timer? _timer;

    public ChargingPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartStatusUpdates();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
    }

    private void StartStatusUpdates()
    {
        _timer = new System.Timers.Timer(5000); // Updates every 5 seconds
        _timer.Elapsed += async (s, e) => await UpdateChargingStatus();
        _timer.AutoReset = true;
        _timer.Start();
    }

    private async Task UpdateChargingStatus()
    {
        if (string.IsNullOrEmpty(ChargerId)) return;

        var status = await _apiService.GetChargingStatusAsync(ChargerId);
        if (status != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                KwhLabel.Text = $"{status.Kwh:F2}";
                BalanceLabel.Text = $"{status.RemainingBalance:C}";
                PowerUsageLabel.Text = $"{status.PowerUsage:F1} kW";

                // Updates battery probression based on kWh (visual effect)
                var progress = Math.Min(1, status.Kwh / 40.0); // Assuming 40 kWh is full
                BatteryProgress.WidthRequest = 150 * progress;
            });
        }
    }

    private async void OnStopChargeClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(ChargerId)) return;

        _timer?.Stop();
        var response = await _apiService.StopChargingAsync(ChargerId);
        if (response != null)
        {
            // Return to MainPage after stopping charge
            await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
        }
        else
        {
            // TODO: Show error message in UI
        }
    }
}