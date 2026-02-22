using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Recuria.Api.Configuration;
using Recuria.Api.Payments;
using Stripe;
using Stripe.Checkout;
using System.Runtime.InteropServices;

namespace Recuria.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public sealed class PaymentsController : ControllerBase
    {
        private readonly StripeOptions _stripe;
        private readonly SessionService _sessions;
        private readonly IStripeWebhookProcessor _processor;
        private readonly IStripeWebhookInbox _inbox;


        public PaymentsController(
            IOptions<StripeOptions> stripe,
            SessionService sessions,
            IStripeWebhookProcessor processor,
            IStripeWebhookInbox inbox)
        {
            _stripe = stripe.Value;
            _sessions = sessions;
            _processor = processor;
            _inbox = inbox;
        }

        public sealed class BillingPlanDto
        {
            public string Code { get; init; } = string.Empty;
            public string Name { get; init; } = string.Empty;
            public long AmountCents { get; init; }
            public string Currency { get; init; } = "usd";
            public string Interval { get; init; } = "month";
        }

        public sealed class CreateCheckoutSessionRequest
        {
            public Guid OrganizationId { get; init; }
            public string PlanCode { get; init; } = string.Empty;
            public int Quantity { get; init; } = 1;
        }


        [HttpPost("checkout-session")]
        [Authorize(Policy = "MemberOrAbove")]
        public async Task<ActionResult<object>> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest req, CancellationToken ct)
        {
            var plan = _stripe.Plans.FirstOrDefault(p =>
                p.Active &&
                string.Equals(p.Code, req.PlanCode, StringComparison.OrdinalIgnoreCase));

            if (plan is null)
                return BadRequest("Invalid plan code.");

            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                SuccessUrl = _stripe.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = _stripe.CancelUrl,
                LineItems = new List<SessionLineItemOptions>
                {
                    new() { Price = plan.StripePriceId, Quantity = req.Quantity }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["org_id"] = req.OrganizationId.ToString(),
                    ["plan_code"] = plan.Code
                }
            };

            if (plan is null)
                return BadRequest("Invalid plan code.");        

            var session = await _sessions.CreateAsync(options, cancellationToken: ct);
            return Ok(new { sessionId = session.Id, url = session.Url });
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook(CancellationToken ct)
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync(ct);
            var signature = Request.Headers["Stripe-Signature"];

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, signature, _stripe.WebhookSecret);
            }
            catch
            {
                return BadRequest();
            }

            await _inbox.EnqueueAsync(stripeEvent.Id, stripeEvent.Type, json, ct);
            return Ok();
        }

        [HttpGet("plans")]
        [AllowAnonymous]
        public ActionResult<IReadOnlyList<BillingPlanDto>> GetPlans()
        {
            var plans = _stripe.Plans
                .Where(p => p.Active)
                .Select(p => new BillingPlanDto
                {
                    Code = p.Code,
                    Name = p.Name,
                    AmountCents = p.AmountCents,
                    Currency = p.Currency,
                    Interval = p.Interval
                })
                .ToList();

            return Ok(plans);
        }
    }
}
