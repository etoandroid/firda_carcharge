using imicCharge.APP.Services; // You can remove this using statement if it's not used elsewhere

namespace imicCharge.APP;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(PaymentPage), typeof(PaymentPage));
        Routing.RegisterRoute(nameof(ChargingPage), typeof(ChargingPage));
    }
}