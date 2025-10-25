using imicCharge.APP.Services;

namespace imicCharge.APP;

[QueryProperty(nameof(CheckoutUrl), "CheckoutUrl")]
public partial class PaymentPage : ContentPage
{
    public string? CheckoutUrl { get; set; }

    public PaymentPage()
    {
        InitializeComponent();
        // Change to use the Navigating event
        StripeWebView.Navigating += OnWebViewNavigating;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (!string.IsNullOrEmpty(CheckoutUrl))
        {
            StripeWebView.Source = CheckoutUrl;
        }
        else
        {
            PopupService.ShowAlertAsync("Feil", "Kunne ikkje laste betalingsside.", "OK");
            Shell.Current.GoToAsync($"//{nameof(MainPage)}");
        }
    }

    private async void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        string currentUrl = e.Url;

        // Check if URL contains signal keywords to determine payment outcome
        if (currentUrl.Contains("payment_success"))
        {
            // Navigate back in the MAUI app
            await Shell.Current.GoToAsync($"//{nameof(MainPage)}");

            // Show success popup
            await PopupService.ShowAlertAsync("Suksess", "Betaling er fullført.");

            e.Cancel = true;
        }
        else if (currentUrl.Contains("payment_cancel"))
        {
            // Navigate back to user account page
            await Shell.Current.GoToAsync($"//{nameof(MainPage)}");

            // Show success popup
            await PopupService.ShowAlertAsync("Betaling ikkje fullført", "Betalinga var avbroten av brukar.");

            e.Cancel = true;
        }
        else
        {
            // Allow Stripe proceed with navigation for other URLs
        }
    }

    // Clean up the event handler when the page disappears
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StripeWebView.Navigating -= OnWebViewNavigating;
        StripeWebView.Source = null;
    }
}