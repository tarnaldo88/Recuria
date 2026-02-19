using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Recuria.Api.Configuration;
using Stripe;
using Stripe.Checkout;
using System.Runtime.InteropServices;

namespace Recuria.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public sealed class PaymentsController : ControllerBase
    {
        private readonly SessionService _sessions;
        private readonly StripeOptions _stripe;

        public PaymentsController(SessionService sessions, StripeOptions stripe)
        {
            _sessions = sessions;
            _stripe = stripe;
        }

        public sealed class CreateCheckoutRequest
        {
            public string PriceId { get; init; } = string.Empty;
            public int Quantity { get; init; } = 1;
            public Guid OrganizationId {  get; init; } = Guid.Empty;
        }

        [HttpPost("checkout-session")]
        [Authorize(Policy = "MemberOrAbove")]
        public async Task<ActionResult<Object>> CreateCheckoutSession([FromBody] CreateCheckoutRequest req, CancellationToken ct)
        {
            var options = new SessionCreateOptions
            { 
                Mode = "subscription",
                SuccessUrl = _stripe.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID",
                CancelUrl = _stripe.CancelUrl,
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = req.PriceId,
                        Quantity = req.Quantity,
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["org_id"] = req.OrganizationId.ToString()
                }
            };

            var session = await _sessions.CreateAsync(options, cancellationToken: ct);
            return Ok(new {sessionId = session.Id, url = session.Url});
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook(CancellationToken ct)
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync(ct);
            var sig = Request.Headers["Stripe-Signature"];
            Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, sig, _stripe.WebhookSecret);
            }
            catch
            {
                return BadRequest();
            }

            if(stripeEvent.Type == "checkout.session.completed")
            {
                //figure this out later
            }

            return Ok();
        }
    }
}
