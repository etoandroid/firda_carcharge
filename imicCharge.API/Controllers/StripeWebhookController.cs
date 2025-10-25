using imicCharge.API.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace imicCharge.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripeWebhookController(
        ILogger<StripeWebhookController> logger,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration) : ControllerBase
    {
        /// <summary>
        /// Handles incoming webhook events from Stripe.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var webhookSecret = configuration["StripeSettings:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
            {
                logger.LogError("[WEBHOOK] Stripe Webhook Secret is NOT CONFIGURED.");
                return StatusCode(500, "Internal server configuration error.");
            }

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json,
                    Request.Headers["Stripe-Signature"], webhookSecret);

                logger.LogInformation("[WEBHOOK] Event received: {EventType}", stripeEvent.Type);

                // KORRIGERT: Brukar den faktiske streng-verdien for hendinga
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    if (stripeEvent.Data.Object is not Session session)
                    {
                        logger.LogError("[WEBHOOK] CRITICAL: Could not deserialize Session object from event.");
                        return BadRequest();
                    }

                    logger.LogInformation("[WEBHOOK] Processing CheckoutSessionCompleted for session ID: {SessionId}", session.Id);

                    if (!session.Metadata.TryGetValue("user_id", out var userId) || string.IsNullOrEmpty(userId))
                    {
                        logger.LogWarning("[WEBHOOK] WARNING: Session {SessionId} is missing 'user_id' in metadata.", session.Id);
                        return Ok();
                    }

                    logger.LogInformation("[WEBHOOK] Found user_id '{UserId}' in metadata. Attempting to find user.", userId);
                    var user = await userManager.FindByIdAsync(userId);

                    if (user == null)
                    {
                        logger.LogWarning("[WEBHOOK] WARNING: User with ID '{UserId}' not found in database.", userId);
                        return Ok();
                    }

                    if (!session.AmountTotal.HasValue)
                    {
                        logger.LogWarning("[WEBHOOK] WARNING: Session {SessionId} has no AmountTotal value.", session.Id);
                        return Ok();
                    }

                    var amountToAdd = (decimal)session.AmountTotal.Value / 100;
                    logger.LogInformation("[WEBHOOK] User '{UserId}' found. Current balance: {Balance}. Amount to add: {Amount}", userId, user.AccountBalance, amountToAdd);

                    user.AccountBalance += amountToAdd;
                    var result = await userManager.UpdateAsync(user);

                    if (result.Succeeded)
                    {
                        logger.LogInformation("[WEBHOOK] SUCCESS: Successfully updated balance for user {UserId}. New balance: {NewBalance}", userId, user.AccountBalance);
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        logger.LogError("[WEBHOOK] FAILED to update balance for user {UserId}. Errors: {Errors}", userId, errors);
                    }
                }
                else
                {
                    logger.LogInformation("[WEBHOOK] Unhandled event type: {EventType}", stripeEvent.Type);
                }

                return Ok();
            }
            catch (StripeException e)
            {
                logger.LogError(e, "[WEBHOOK] Stripe signature verification failed. Check your WebhookSecret.");
                return BadRequest();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WEBHOOK] An unexpected error occurred.");
                return StatusCode(500);
            }
        }
    }
}

