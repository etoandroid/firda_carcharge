using imicCharge.API.Models;
using System.Text.Json;

namespace imicCharge.API.Services;

public interface IEaseeService
{
    Task<IEnumerable<EaseeCharger>?> GetChargersAsync();
    Task<bool> StartChargingAsync(string chargerId);
    Task<bool> StopChargingAsync(string chargerId);
    Task<OngoingSession?> GetOngoingSessionAsync(string chargerId);
    Task<JsonElement?> GetChargerStateAsync(string chargerId);
}