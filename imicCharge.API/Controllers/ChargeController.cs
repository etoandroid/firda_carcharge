using imicCharge.API.Data;
using imicCharge.API.Extensions;
using imicCharge.API.Models;
using imicCharge.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace imicCharge.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChargeController : ControllerBase // Remove the primary constructor syntax if you had it
    {
        // Define fields for dependencies ---
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ChargeController> _logger;
        private readonly IEaseeService _easeeService; // Use the interface type

        public ChargeController(
            UserManager<ApplicationUser> userManager,
            ILogger<ChargeController> logger,
            IEaseeService easeeService)
        {
            _userManager = userManager;
            _logger = logger;
            _easeeService = easeeService;
        }

        /// <summary>
        /// Initiates a charging session for the authenticated user on a specified charger.
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartCharging([FromBody] StartChargingRequest request)
        {
            var user = await _getCurrentUserAsync();
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (user.AccountBalance <= 0)
            {
                return BadRequest(new { error = "Du har for låg saldo. Fyll på kontoen din før du startar lading." });
            }

            _logger.LogInformation("User {UserId} starting charge on {ChargerId}. Balance: {Balance}", user.Id, request.ChargerId, user.AccountBalance);
            var success = await _easeeService.StartChargingAsync(request.ChargerId); // Uses the interface field

            if (success)
            {
                return Ok(new { message = $"Ladeførespurnad for ladar {request.ChargerId} er sendt." });
            }

            return StatusCode(500, new { error = $"Klarte ikkje å starte lading for ladar {request.ChargerId}." });
        }

        /// <summary>
        /// Stops the current charging session and processes the final payment.
        /// </summary>
        [HttpPost("stop")]
        public async Task<IActionResult> StopCharging([FromBody] StopChargingRequest request)
        {
            var user = await _getCurrentUserAsync();
            if (user == null)
            {
                return NotFound("User not found.");
            }

            _logger.LogInformation("User {UserId} stopping charge on {ChargerId}", user.Id, request.ChargerId);

            var session = await _easeeService.GetOngoingSessionAsync(request.ChargerId); // Uses the interface field
            if (session == null)
            {
                _logger.LogWarning("Could not retrieve ongoing charging session for charger {ChargerId} before stopping.", request.ChargerId);
            }

            var success = await _easeeService.StopChargingAsync(request.ChargerId); // Uses the interface field

            if (!success)
            {
                return StatusCode(500, new { error = $"Klarte ikkje å stoppe lading for ladar {request.ChargerId}." });
            }

            if (session?.CostIncludingVat != null)
            {
                var cost = (decimal)session.CostIncludingVat.Value;
                // Prevent negative cost issues from mock timing
                if (cost < 0) cost = 0;
                user.AccountBalance -= cost;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Charged user {UserId} for {Kwh} kWh. Cost: {Cost}. New balance: {Balance}", user.Id, session.SessionEnergy, cost, user.AccountBalance);

                return Ok(new
                {
                    message = $"Lading stoppa. Du har blitt belasta {cost:C} for {session.SessionEnergy:F2} kWh.",
                    newBalance = user.AccountBalance
                });
            }
            return Ok(new { message = "Lading stoppa. Saldoen din vil bli oppdatert om kort tid." });
        }

        /// <summary>
        /// Gets a list of all chargers available to the authenticated user.
        /// </summary>
        [HttpGet("chargers")]
        public async Task<IActionResult> GetChargers()
        {
            var chargers = await _easeeService.GetChargersAsync(); // Uses the interface field
            if (chargers == null)
            {
                return NotFound("Kunne ikkje hente ladarar.");
            }
            return Ok(chargers);
        }

        /// <summary>
        /// Retrieves ongoing/live charging session details for a specific charger.
        /// </summary>
        [HttpGet("status/{chargerId}")]
        public async Task<IActionResult> GetChargingStatus(string chargerId)
        {
            var user = await _getCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            var session = await _easeeService.GetOngoingSessionAsync(chargerId); // Uses the interface field
            var chargerState = await _easeeService.GetChargerStateAsync(chargerId); // Uses the interface field

            double sessionEnergy = session?.SessionEnergy ?? 0.0;
            decimal cost = (decimal)(session?.CostIncludingVat ?? 0.0);
            // Prevent negative cost issues from mock timing
            if (cost < 0) cost = 0;
            decimal remainingBalance = user.AccountBalance - cost;
            double powerUsage = 0.0;
            if (chargerState.HasValue && chargerState.Value.TryGetProperty("chargerPow", out var powerElement))
            {
                powerUsage = powerElement.GetDouble();
            }

            return Ok(new
            {
                Kwh = sessionEnergy,
                RemainingBalance = remainingBalance,
                PowerUsage = powerUsage
            });
        }

        /// <summary>
        /// Helper method to get the current authenticated user.
        /// </summary>
        private async Task<ApplicationUser?> _getCurrentUserAsync()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }
            return await _userManager.FindByIdAsync(userId);
        }
    }
}