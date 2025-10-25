using imicCharge.API.Data;
using imicCharge.API.Extensions;
using imicCharge.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace imicCharge.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController(
        SessionService sessionService,
        UserManager<ApplicationUser> userManager,
        ILogger<PaymentController> logger) : ControllerBase
    {
        /// <summary>
        /// Retrieves the current account balance for the authenticated user.
        /// </summary>
        /// <returns>An object containing the user's account balance.</returns>
        [Authorize]
        [HttpGet("get-account-balance")]
        public async Task<IActionResult> GetAccountBalance()
        {
            try
            {
                var userId = User.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Kunne ikkje identifisere brukar.");
                }

                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Unauthorized("Brukar ikkje funnen.");
                }

                return Ok(new { accountBalance = user.AccountBalance });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred in GetAccountBalance.");
                return StatusCode(500, new { error = "Ein intern feil oppstod ved henting av saldo." });
            }
        }

        /// <summary>
        /// Creates a new Stripe Checkout Session for the authenticated user.
        /// </summary>
        /// <returns>A URL to the Stripe-hosted checkout page.</returns>
        [Authorize]
        [HttpPost("create-checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreatePaymentRequest request)
        {
            var userId = User.GetUserId();
            var user = await userManager.FindByIdAsync(userId!);
            if (user == null)
            {
                return Unauthorized("Brukar ikkje funnen.");
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card", "mobilepay" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(request.Amount * 100),
                            Currency = "nok",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Påfylling av ladekonto",
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                CustomerEmail = user.Email,
                SuccessUrl = "https://imiccharge.app/payment_success",
                CancelUrl = "https://imiccharge.app/payment_cancel",
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", user.Id }
                }
            };

            try
            {
                var session = await sessionService.CreateAsync(options);
                return Ok(new { url = session.Url });
            }
            catch (StripeException e)
            {
                logger.LogError(e, "Stripe error: {StripeError}", e.StripeError.Message);
                return BadRequest(new { error = e.StripeError.Message });
            }
        }
    }
}