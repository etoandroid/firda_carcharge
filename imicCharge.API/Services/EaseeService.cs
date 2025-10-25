using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace imicCharge.API.Services;

/// <summary>
/// Represents live data from an ongoing charging session.
/// </summary>
/// <summary>
/// Represents live data from an ongoing charging session based on Easee API.
/// </summary>
public class OngoingSession
{
    [JsonPropertyName("chargerId")]
    public string? ChargerId { get; set; }

    [JsonPropertyName("sessionEnergy")]
    public double SessionEnergy { get; set; } // kWh

    [JsonPropertyName("sessionStart")]
    public DateTimeOffset? SessionStart { get; set; } // Use DateTimeOffset for time zone handling

    [JsonPropertyName("sessionEnd")]
    public DateTimeOffset? SessionEnd { get; set; }

    [JsonPropertyName("sessionId")]
    public long? SessionId { get; set; } // Use long for potentially large IDs

    [JsonPropertyName("chargeDurationInSeconds")]
    public int? ChargeDurationInSeconds { get; set; }

    [JsonPropertyName("firstEnergyTransferPeriodStart")]
    public DateTimeOffset? FirstEnergyTransferPeriodStart { get; set; }

    [JsonPropertyName("lastEnergyTransferPeriodEnd")]
    public DateTimeOffset? LastEnergyTransferPeriodEnd { get; set; }

    [JsonPropertyName("pricePrKwhIncludingVat")]
    public double? PricePrKwhIncludingVat { get; set; }

    [JsonPropertyName("pricePerKwhExcludingVat")]
    public double? PricePerKwhExcludingVat { get; set; }

    [JsonPropertyName("vatPercentage")]
    public double? VatPercentage { get; set; }

    [JsonPropertyName("currencyId")]
    public string? CurrencyId { get; set; }

    [JsonPropertyName("costIncludingVat")]
    public double? CostIncludingVat { get; set; }

    [JsonPropertyName("costExcludingVat")]
    public double? CostExcludingVat { get; set; }
}

/// <summary>
/// Represents a single Easee charger.
/// </summary>
public class EaseeCharger
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// Handles all communication with the Easee API for charging operations.
/// </summary>
public class EaseeService : IEaseeService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private string? _accessToken;
    private DateTime _tokenExpiryTime;

    public EaseeService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    /// <summary>
    /// Authenticates with the Easee API if the current access token is missing or expired.
    /// </summary>
    private async Task AuthenticateIfNeededAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiryTime)
        {
            return; // Token is still valid
        }

        var easeeSettings = _configuration.GetSection("EaseeSettings");
        var username = easeeSettings["Username"];
        var password = easeeSettings["Password"];

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException("Easee username or password is not configured in secrets.");
        }

        // Create a dedicated client for authentication from the factory.
        var authClient = _httpClientFactory.CreateClient("easee-auth");

        var credentials = new { userName = username, password };
        var content = new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, "application/json");

        var response = await authClient.PostAsync("api/accounts/login", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

        _accessToken = tokenResponse.GetProperty("accessToken").GetString();
        var expiresIn = tokenResponse.GetProperty("expiresIn").GetInt32();
        _tokenExpiryTime = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Subtract 60 seconds for safety margin
    }

    /// <summary>
    /// Creates and configures a new HttpClient for standard API calls.
    /// This is a helper method to avoid code repetition.
    /// </summary>
    private HttpClient CreateApiClient()
    {
        var client = _httpClientFactory.CreateClient("easee-api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return client;
    }

    /// <summary>
    /// Retrieves a list of all chargers available to the user.
    /// </summary>
    public async Task<IEnumerable<EaseeCharger>?> GetChargersAsync()
    {
        await AuthenticateIfNeededAsync();
        var client = CreateApiClient();

        return await client.GetFromJsonAsync<List<EaseeCharger>>("api/chargers");
    }

    /// <summary>
    /// Sends a command to start charging a specific charger.
    /// </summary>
    /// <param name="chargerId">The ID of the charger to start.</param>
    /// <returns>True if the command was successful, otherwise false.</returns>
    public async Task<bool> StartChargingAsync(string chargerId)
    {
        await AuthenticateIfNeededAsync();
        var client = CreateApiClient();
        var response = await client.PostAsync($"api/chargers/{chargerId}/commands/start_charging", null);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Sends a command to stop charging a specific charger.
    /// </summary>
    /// <param name="chargerId">The ID of the charger to stop.</param>
    /// <returns>True if the command was successful, otherwise false.</returns>
    public async Task<bool> StopChargingAsync(string chargerId)
    {
        await AuthenticateIfNeededAsync();
        var client = CreateApiClient();
        var response = await client.PostAsync($"api/chargers/{chargerId}/commands/stop_charging", null);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Retrieves the state of an ongoing charging session.
    /// </summary>
    /// <param name="chargerId">The ID of the charger.</param>
    /// <returns>An OngoingSession object with live data, or null if not found.</returns>
    public async Task<OngoingSession?> GetOngoingSessionAsync(string chargerId)
    {
        await AuthenticateIfNeededAsync();
        var client = CreateApiClient();

        try
        {
            var response = await client.GetAsync($"api/chargers/{chargerId}/sessions/ongoing");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<OngoingSession>();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // 404 error: Charging hasn't started yet or session doesn't exist? Return null.
                return null;
            }
            else
            {
                // Throw and exception for other errors to indicate a real problem
                response.EnsureSuccessStatusCode();
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            // TODO: Consider more specific/proper logging based on ex.StatusCode if available.
            Console.WriteLine($"Error fetching ongoing session: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the real-time state of a specific charger.
    /// </summary>
    /// <returns>A JsonElement representing the charger's state, or null if not found.</returns>
    public async Task<JsonElement?> GetChargerStateAsync(string chargerId)
    {
        await AuthenticateIfNeededAsync();
        var client = CreateApiClient();
        return await client.GetFromJsonAsync<JsonElement>($"api/chargers/{chargerId}/state");
    }
}