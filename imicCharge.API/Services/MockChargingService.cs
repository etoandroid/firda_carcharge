using imicCharge.API.Models;
using System.Text.Json;

namespace imicCharge.API.Services;

/// <summary>
/// Mock service focusing ONLY on simulating charging start, stop, and ongoing data.
/// Delegates GetChargersAsync to the real service.
/// </summary>
public class MockChargingService : IEaseeService // Implementer interface
{
    private readonly EaseeService _realEaseeService; // Inject the REAL service
    private readonly IConfiguration _configuration;
    private readonly ILogger<MockChargingService> _logger;

    // --- Simulation State ---
    private static bool _isMockCharging = false;
    private static string? _mockChargingChargerId = null;
    private static DateTimeOffset? _mockSessionStartTime;
    private static double _mockCurrentEnergy = 0.0;
    private static readonly double _mockPowerDrawKw = 7.4;
    private static readonly long _mockSessionId = 55667788;
    private static readonly decimal _simulatedVatPercentage = 25.0m;

    public MockChargingService(
        EaseeService realEaseeService, // Inject the concrete real service
        IConfiguration configuration,
        ILogger<MockChargingService> logger)
    {
        _realEaseeService = realEaseeService;
        _configuration = configuration;
        _logger = logger;
    }

    // --- DELEGATED METHOD ---
    public Task<IEnumerable<EaseeCharger>?> GetChargersAsync()
    {
        _logger.LogInformation("MOCK WRAPPER: Delegating GetChargersAsync to real service.");
        // Kall den ekte metoden frå den injecta EaseeService
        return _realEaseeService.GetChargersAsync();
    }

    // --- MOCKED METHODS ---
    public Task<bool> StartChargingAsync(string chargerId)
    {
        _logger.LogInformation("MOCK: Simulating StartCharging for {ChargerId}", chargerId);
        _isMockCharging = true;
        _mockChargingChargerId = chargerId;
        _mockSessionStartTime = DateTimeOffset.UtcNow;
        _mockCurrentEnergy = 0.0;
        return Task.FromResult(true); // Always succeed in mock
    }

    public Task<bool> StopChargingAsync(string chargerId)
    {
        _logger.LogInformation("MOCK: Simulating StopCharging for {ChargerId}", chargerId);
        if (_mockChargingChargerId == chargerId)
        {
            _isMockCharging = false;
            _mockChargingChargerId = null;
            _mockSessionStartTime = null;
        }
        return Task.FromResult(true); // Always succeed in mock
    }

    public Task<OngoingSession?> GetOngoingSessionAsync(string chargerId)
    {
        if (!_isMockCharging || _mockChargingChargerId != chargerId || _mockSessionStartTime == null)
        {
            _logger.LogInformation("MOCK: No active mock session for GetOngoingSessionAsync({ChargerId}). Returning null.", chargerId);
            return Task.FromResult<OngoingSession?>(null);
        }

        // --- Simulate Realistic Data ---
        var now = DateTimeOffset.UtcNow;
        var timeElapsed = now - _mockSessionStartTime.Value;
        _mockCurrentEnergy = _mockPowerDrawKw * timeElapsed.TotalHours;
        var pricePerKwhInclVatDecimal = _configuration.GetValue<decimal>("ChargingSettings:PricePerKwh", 2.50m);
        var pricePerKwhInclVat = (double)pricePerKwhInclVatDecimal;
        var pricePerKwhExclVat = pricePerKwhInclVat / (1 + ((double)_simulatedVatPercentage / 100.0));
        var costInclVat = _mockCurrentEnergy * pricePerKwhInclVat;
        var costExclVat = _mockCurrentEnergy * pricePerKwhExclVat;

        var session = new OngoingSession
        { /* ... fyll ut alle felt som før ... */
            ChargerId = chargerId,
            SessionId = _mockSessionId,
            SessionStart = _mockSessionStartTime.Value,
            SessionEnd = null,
            FirstEnergyTransferPeriodStart = _mockSessionStartTime.Value.AddSeconds(1),
            LastEnergyTransferPeriodEnd = now.AddSeconds(-1),
            SessionEnergy = Math.Round(_mockCurrentEnergy, 4),
            ChargeDurationInSeconds = (int)timeElapsed.TotalSeconds,
            PricePrKwhIncludingVat = pricePerKwhInclVat,
            PricePerKwhExcludingVat = Math.Round(pricePerKwhExclVat, 4),
            VatPercentage = (double)_simulatedVatPercentage,
            CurrencyId = "NOK",
            CostIncludingVat = Math.Round(costInclVat, 2),
            CostExcludingVat = Math.Round(costExclVat, 2)
        };
        _logger.LogInformation("MOCK: Providing mocked ongoing session data for {ChargerId}.", chargerId);
        return Task.FromResult<OngoingSession?>(session);
    }

    public Task<JsonElement?> GetChargerStateAsync(string chargerId)
    {
        double currentPower = (_isMockCharging && _mockChargingChargerId == chargerId) ? _mockPowerDrawKw : 0.0;
        _logger.LogInformation("MOCK: Providing mocked charger state for {ChargerId}. Power: {Power} kW", chargerId, currentPower);
        var state = new { chargerPow = currentPower };
        var json = JsonSerializer.Serialize(state);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        return Task.FromResult<JsonElement?>(jsonElement);
    }
}