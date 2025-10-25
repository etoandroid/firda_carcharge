using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace imicCharge.APP.Services;

public class LoginResponse
{
    public string? TokenType { get; set; }
    public string? AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    public string? RefreshToken { get; set; }
}

public class StopChargingResponse
{
    public string? Message { get; set; }
    public decimal NewBalance { get; set; }
}

public class ChargingStatusResponse
{
    public double Kwh { get; set; }
    public decimal RemainingBalance { get; set; }
    public double PowerUsage { get; set; }
}

public class EaseeCharger
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

public class ApiService
{
    private readonly string _baseUrl;

    /// <summary>
    /// Initialize the API service with the base URL for the backend.
    /// </summary>
    public ApiService()
    {
        _baseUrl = "https://api.firdacar.no:443";
    }

    /// <summary>
    /// Attempts to log in a user with the provided credentials.
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var client = GetHttpClient();
        var loginData = new { email, password };

        try
        {
            var response = await client.PostAsJsonAsync("login", loginData);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LoginResponse>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Attempts to register a new user.
    /// </summary>
    public async Task<bool> RegisterAsync(string email, string password)
    {
        var client = GetHttpClient();
        var registerData = new { email, password };

        try
        {
            var response = await client.PostAsJsonAsync("register", registerData);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration error: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// Retrieves the account balance for the currently authenticated user.
    /// </summary>
    public async Task<decimal?> GetAccountBalanceAsync()
    {
        var client = GetHttpClient();
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrEmpty(token)) return null;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.GetAsync("api/Payment/get-account-balance");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("accountBalance", out var balanceElement))
                {
                    return balanceElement.GetDecimal();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching balance: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Retrieves a list of all chargers available to the user.
    /// </summary>
    public async Task<List<EaseeCharger>?> GetChargersAsync()
    {
        var client = GetHttpClient();
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrEmpty(token)) return null;

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.GetAsync("api/Charge/chargers");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<EaseeCharger>>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching chargers: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Sends a request to the API to start a charging session.
    /// </summary>
    public async Task<bool> StartChargingAsync(string chargerId)
    {
        var client = GetHttpClient();
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrEmpty(token)) return false;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var chargeRequest = new { chargerId };

        try
        {
            var response = await client.PostAsJsonAsync("api/Charge/start", chargeRequest);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting charge: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sends a request to the API to stop a charging session.
    /// </summary>
    public async Task<StopChargingResponse?> StopChargingAsync(string chargerId)
    {
        var client = GetHttpClient();
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrEmpty(token)) return null;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var chargeRequest = new { chargerId };

        try
        {
            var response = await client.PostAsJsonAsync("api/Charge/stop", chargeRequest);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<StopChargingResponse>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping charge: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Retrieves live charging status from the API.
    /// </summary>
    public async Task<ChargingStatusResponse?> GetChargingStatusAsync(string chargerId)
    {
        var client = GetHttpClient();
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrEmpty(token)) return null;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.GetAsync($"api/Charge/status/{chargerId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ChargingStatusResponse>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching charging status: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Asks the API for a Stripe Checkout session URL.
    /// </summary>
    public async Task<string?> CreateCheckoutSessionAsync(decimal amount)
    {
        var client = GetHttpClient();
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrEmpty(token)) return null;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var paymentRequest = new { amount };

        try
        {
            var response = await client.PostAsJsonAsync("api/Payment/create-checkout-session", paymentRequest);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("url", out var urlElement))
                {
                    return urlElement.GetString();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating checkout session: {ex.Message}");
        }
        return null;
    }

    public HttpClient GetHttpClient()
    {
        return new HttpClient { BaseAddress = new Uri(_baseUrl) };
    }
}