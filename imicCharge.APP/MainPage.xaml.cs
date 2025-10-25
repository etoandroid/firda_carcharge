using imicCharge.APP.Services;

namespace imicCharge.APP;

public partial class MainPage : ContentPage
{
    private readonly ApiService _apiService;
    private decimal _currentBalance = 0;

    public MainPage()
    {
        InitializeComponent();
        _apiService = new ApiService();

        StartChargeButton.Clicked += OnStartChargeClicked;
        TopUpButton.Clicked += OnTopUpClicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        PopupService.Register(AlertView);
        LoadingService.Register(BusyIndicator);

        await LoadingService.ShowAsync("Hentar kontoinformasjon..");

        try
        {
            Task userInfoTask = UpdateUserInfo(); // Update UI with user info
            Task balanceTask = UpdateBalanceDisplay(); // Load acccound balance
            Task chargersTask = LoadChargers(); // Load charger list

            await Task.WhenAll(userInfoTask, balanceTask, chargersTask);
            await LoadingService.HideAsync();
        }
        catch (Exception) // Catch potential aggregate exceptions
        {
            // TODO: Add sophisticated error handling and user feedback?
            // TODO: Add error logging
            await LoadingService.HideAsync();
            await PopupService.ShowAlertAsync("Feil", "Kunne ikkje laste kontoinformasjon.");
        }
    }

    private async Task UpdateUserInfo()
    {
        try
        {
            AccountName.Text = await SecureStorage.GetAsync("user_email"); // Set account email address in UI
        }
        catch (Exception)
        {
            // TODO: Consider error logging/handling and adding user feedback
            AccountName.Text = "Feil";
            throw;
        }
    }

    private async Task UpdateBalanceDisplay()
    {
        try
        {
            var balance = await _apiService.GetAccountBalanceAsync();
            if (balance.HasValue)
            {
                _currentBalance = balance.Value;
                BalanceLabel.Text = $"{_currentBalance:C}"; // Format as currency/NOK ("kr 123,45")
            }
            else
            {
                // TODO: Consider error logging/handling and adding user feedback
                BalanceLabel.Text = "Feil";
            }
        }
        catch (Exception)
        {
            // TODO: Consider error logging/handling and adding user feedback
            BalanceLabel.Text = "Feil";
            throw;
        }
    }

    private async Task LoadChargers()
    {
        try
        {
            var chargers = await _apiService.GetChargersAsync();
            if (chargers != null)
            {
                ChargerPicker.ItemsSource = chargers;
            }
            else
            {
                // TODO: Consider logging this error or informing users that no active chargers exist at site
            }
        }
        catch (Exception)
        {
            // TODO: Consider error logging/handling and adding user feedback
            throw;
        }
    }

    private void OnLogoutClicked(object? sender, EventArgs e)
    {
        SecureStorage.Remove("access_token");
        SecureStorage.Remove("user_email");
        if (Application.Current?.Windows.FirstOrDefault() is Window window)
        {
            window.Page = new LoginPage();
        }
    }

    private async void OnStartChargeClicked(object? sender, EventArgs e)
    {
        // Validation 1: Check if a charger is selected
        var selectedCharger = ChargerPicker.SelectedItem as EaseeCharger;

        if (selectedCharger == null)
        {
            await PopupService.ShowAlertAsync("Ingen ladar vald", "Du må velje ein ladar frå lista.");
            return;
        }

        // Validation 2: Check if balance is sufficient. Balance must be at least 20 NOK to allow charging.
        if (_currentBalance < 20)
        {
            await PopupService.ShowAlertAsync("For låg saldo", "Saldoen din er under kr 20. Fyll på kontoen før du startar lading.");
            return;
        }

        if (string.IsNullOrEmpty(selectedCharger.Id))
        {
            await PopupService.ShowAlertAsync("Feil", "Den valde ladaren har ingen gyldig ID.");
            return;
        }

        await LoadingService.ShowAsync("Startar lading...");
        var success = false;

        try {
            success = await _apiService.StartChargingAsync(selectedCharger.Id);
            if (success)
            {
                await LoadingService.HideAsync();
                await Shell.Current.GoToAsync($"{nameof(ChargingPage)}?ChargerId={selectedCharger.Id}");
            }
            else
            {
                await LoadingService.HideAsync();
                await PopupService.ShowAlertAsync("Feil", "Kunne ikkje starte lading. Prøv igjen seinare.");
            }
        }
        catch (Exception)
        {
            await LoadingService.HideAsync();
            await PopupService.ShowAlertAsync("Feil", "Ein ukjend feil oppstod ved start av lading.");
        }
    }

    private async void OnTopUpClicked(object? sender, EventArgs e)
    {
        // Keep showing the modal until a valid amount is entered or cancelled
        string lastEnteredAmount = ""; // Store invalid amount to show again
        while (true)
        {
            // Show the modal, passing back any previously entered invalid amount
            decimal? amount = await TopUpDialog.Show(lastEnteredAmount);
            lastEnteredAmount = TopUpDialog.AmountText; // Update last entered before hiding potentially

            if (amount == null) // User cancelled or entered invalid text
            {
                await TopUpDialog.Hide(clearEntry: true); // Ensure it's hidden and cleared on cancel
                break; // Exit the loop
            }

            // Validate the amount
            if (amount < 50 || amount > 2000)
            {
                // Hide the modal *temporarily* while showing the alert
                await TopUpDialog.Hide(clearEntry: false); // Don't clear the entry

                await PopupService.ShowAlertAsync("Ugyldig beløp", "Beløpet må vera mellom kr 50 og kr 2000.");
                // Loop will continue, showing the modal again with lastEnteredAmount
            }
            else // Amount is valid
            {
                // Hide the modal permanently now
                await TopUpDialog.Hide(clearEntry: true);

                // Proceed to payment
                await LoadingService.ShowAsync("Gjer klar betaling...");
                string? checkoutUrl = null;
                try
                {
                    checkoutUrl = await _apiService.CreateCheckoutSessionAsync(amount.Value);
                }
                catch (Exception)
                {
                    // TODO: Log exception
                    await LoadingService.HideAsync();
                    await PopupService.ShowAlertAsync("Feil", "Kunne ikkje starta betaling. Prøv igjen seinare.");
                    break; // Exit loop on payment setup error
                }


                if (string.IsNullOrEmpty(checkoutUrl))
                {
                    await LoadingService.HideAsync();
                    await PopupService.ShowAlertAsync("Feil", "Kunne ikkje starta betaling. Prøv igjen seinare.");
                }
                else
                {
                    // Navigate to Payment Page (Loading indicator hides implicitly on navigation)
                    await Shell.Current.GoToAsync(nameof(PaymentPage), new Dictionary<string, object>
                    {
                        { "CheckoutUrl", checkoutUrl }
                    });
                }
                await LoadingService.HideAsync(); // Ensure it hides if navigation fails or is quick
                break; // Exit the loop
            }
        } // End while loop
    }
}