using imicCharge.APP.Services;

namespace imicCharge.APP;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;

    public LoginPage()
    {
        InitializeComponent();
        _apiService = new ApiService();

        // Register the specific CustomAlert instance from this page.
        PopupService.Register(AlertView);
        LoadingService.Register(BusyIndicator);
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text;
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await PopupService.ShowAlertAsync("Feil", "E-post og passord kan ikkje vere tomme.");
            return;
        }

        await LoadingService.ShowAsync("Loggar inn..");

        try
        {
            var loginResponse = await _apiService.LoginAsync(email, password);

            if (loginResponse?.AccessToken != null)
            {
                await SecureStorage.SetAsync("access_token", loginResponse.AccessToken);
                await SecureStorage.SetAsync("user_email", email);

                if (Application.Current?.Windows[0] != null)
                {
                    Application.Current.Windows[0].Page = new AppShell();
                }
            }
            else
            {
                await LoadingService.HideAsync();
                await PopupService.ShowAlertAsync("Innlogging feila", "Sjekk at e-post og passord er korrekt.");
            }
        }
        catch (Exception)
        {
            await LoadingService.HideAsync();
            await PopupService.ShowAlertAsync("Feil", "Ein feil oppstod under innlogging. Prøv igjen.");
        }
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text;
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await PopupService.ShowAlertAsync("Feil", "E-post og passord kan ikkje vere tomme.");
            return;
        }

        await LoadingService.ShowAsync("Registrerer..");

        try
        {
            bool success = await _apiService.RegisterAsync(email, password);

            if (success)
            {
                await LoadingService.HideAsync();
                await PopupService.ShowAlertAsync("Suksess", "Brukaren din er no registrert. Ver venleg og logg inn.");
            }
            else
            {
                await LoadingService.HideAsync();
                await PopupService.ShowAlertAsync("Registrering feila", "E-postadressa er kanskje allereie i bruk. Prøv ei anna.");
            }
        }
        catch (Exception)
        {
            await LoadingService.HideAsync();
            await PopupService.ShowAlertAsync("Feil", "Ein feil oppstod under registrering. Prøv igjen.");
        }
    }

    private void OnLoginTriggered(object? sender, EventArgs e)
    {
        OnLoginClicked(sender, e);
    }

}