using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Recuria.Api.Auth;
using Recuria.Api.Configuration;
using Recuria.Api.Payments;
using Recuria.Application.Contracts.Common;
using Recuria.Infrastructure.Persistence;
using Stripe;
using Stripe.Checkout;

namespace Recuria.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public sealed class PaymentsController : ControllerBase
    {
        private readonly StripeOptions _stripe;
        private readonly SessionService _sessions;
        private readonly IStripeWebhookInbox _inbox;
        private readonly RecuriaDbContext _db;

        public PaymentsController(
            IOptions<StripeOptions> stripe,
            SessionService sessions,
            IStripeWebhookInbox inbox,
            RecuriaDbContext db)
        {
            _stripe = stripe.Value;
            _sessions = sessions;
            _inbox = inbox;
            _db = db;
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

        public sealed class CreatePortalSessionRequest
        {
            public Guid OrganizationId { get; init; }
        }

        public sealed class CreatePortalSessionResponse
        {
            public string Url { get; init; } = string.Empty;
        }

        public sealed class StripeWebhookInboxItemDto
        {
            public Guid Id { get; init; }
            public string StripeEventId { get; init; } = string.Empty;
            public string EventType { get; init; } = string.Empty;
            public DateTime ReceivedOnUtc { get; init; }
            public DateTime? ProcessedOnUtc { get; init; }
            public int AttemptCount { get; init; }
            public DateTime? NextAttemptOnUtc { get; init; }
            public string? LastError { get; init; }
            public string Status { get; init; } = string.Empty;
        }

        public sealed class StripeWebhookSummaryDto
        {
            public int ProcessedCount { get; init; }
            public int PendingCount { get; init; }
            public int RetryingCount { get; init; }
            public int DeadLetterLikeCount { get; init; }
        }

        [HttpPost("checkout-session")]
        [Authorize(Policy = AuthorizationPolicies.PaymentsCheckout)]
        public async Task<ActionResult<object>> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest req, CancellationToken ct)
        {
            if (!User.IsInOrganization(req.OrganizationId))
                return Forbid();

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

            var session = await _sessions.CreateAsync(options, cancellationToken: ct);
            return Ok(new { sessionId = session.Id, url = session.Url });
        }

        [HttpPost("portal-session")]
        [Authorize(Policy = AuthorizationPolicies.PaymentsCheckout)]
        public async Task<ActionResult<CreatePortalSessionResponse>> CreatePortalSession([FromBody] CreatePortalSessionRequest req, CancellationToken ct)
        {
            if (!User.IsInOrganization(req.OrganizationId))
                return Forbid();

            var map = await _db.StripeSubscriptionMaps
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrganizationId == req.OrganizationId, ct);

            if (map is null || string.IsNullOrWhiteSpace(map.StripeCustomerId))
                return BadRequest("Stripe customer mapping was not found for this organization.");

            var portal = new Stripe.BillingPortal.SessionService();
            var returnUrl = string.IsNullOrWhiteSpace(_stripe.PortalReturnUrl) ? _stripe.CancelUrl : _stripe.PortalReturnUrl;
            var session = await portal.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = map.StripeCustomerId,
                ReturnUrl = returnUrl
            }, cancellationToken: ct);

            return Ok(new CreatePortalSessionResponse { Url = session.Url });
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

        [HttpGet("webhook-summary")]
        [Authorize(Policy = AuthorizationPolicies.OpsManage)]
        public async Task<ActionResult<StripeWebhookSummaryDto>> GetWebhookSummary(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var processedCount = await _db.StripeWebhookInboxMessages.CountAsync(x => x.ProcessedOnUtc != null, ct);
            var pendingCount = await _db.StripeWebhookInboxMessages.CountAsync(x => x.ProcessedOnUtc == null && x.AttemptCount == 0, ct);
            var retryingCount = await _db.StripeWebhookInboxMessages.CountAsync(x =>
                x.ProcessedOnUtc == null &&
                x.AttemptCount > 0 &&
                (x.NextAttemptOnUtc == null || x.NextAttemptOnUtc > now), ct);
            var deadLetterLikeCount = await _db.StripeWebhookInboxMessages.CountAsync(x =>
                x.ProcessedOnUtc == null &&
                x.AttemptCount >= 10, ct);

            return Ok(new StripeWebhookSummaryDto
            {
                ProcessedCount = processedCount,
                PendingCount = pendingCount,
                RetryingCount = retryingCount,
                DeadLetterLikeCount = deadLetterLikeCount
            });
        }

        [HttpGet("webhook-inbox")]
        [Authorize(Policy = AuthorizationPolicies.OpsManage)]
        public async Task<ActionResult<PagedResult<StripeWebhookInboxItemDto>>> GetWebhookInbox(
            [FromQuery] TableQuery query,
            CancellationToken ct)
        {
            var safe = new TableQuery
            {
                Page = Math.Max(1, query.Page),
                PageSize = Math.Clamp(query.PageSize, 5, 200),
                Search = query.Search,
                SortBy = query.SortBy,
                SortDir = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc"
            };

            var q = _db.StripeWebhookInboxMessages.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(safe.Search))
            {
                var s = safe.Search.Trim();
                q = q.Where(x => x.EventType.Contains(s) || x.StripeEventId.Contains(s) || (x.LastError ?? string.Empty).Contains(s));
            }

            q = (safe.SortBy?.ToLowerInvariant(), safe.SortDir) switch
            {
                ("eventtype", "desc") => q.OrderByDescending(x => x.EventType),
                ("eventtype", _) => q.OrderBy(x => x.EventType),
                ("attemptcount", "desc") => q.OrderByDescending(x => x.AttemptCount),
                ("attemptcount", _) => q.OrderBy(x => x.AttemptCount),
                ("processedonutc", "asc") => q.OrderBy(x => x.ProcessedOnUtc),
                ("processedonutc", _) => q.OrderByDescending(x => x.ProcessedOnUtc),
                ("receivedonutc", "asc") => q.OrderBy(x => x.ReceivedOnUtc),
                _ => q.OrderByDescending(x => x.ReceivedOnUtc)
            };

            var total = await q.CountAsync(ct);
            var now = DateTime.UtcNow;
            var items = await q
                .Skip((safe.Page - 1) * safe.PageSize)
                .Take(safe.PageSize)
                .Select(x => new StripeWebhookInboxItemDto
                {
                    Id = x.Id,
                    StripeEventId = x.StripeEventId,
                    EventType = x.EventType,
                    ReceivedOnUtc = x.ReceivedOnUtc,
                    ProcessedOnUtc = x.ProcessedOnUtc,
                    AttemptCount = x.AttemptCount,
                    NextAttemptOnUtc = x.NextAttemptOnUtc,
                    LastError = x.LastError,
                    Status = x.ProcessedOnUtc != null
                        ? "Processed"
                        : x.AttemptCount >= 10
                            ? "DeadLetterLike"
                            : x.AttemptCount > 0 && x.NextAttemptOnUtc > now
                                ? "Retrying"
                                : "Pending"
                })
                .ToListAsync(ct);

            return Ok(new PagedResult<StripeWebhookInboxItemDto>
            {
                Items = items,
                Page = safe.Page,
                PageSize = safe.PageSize,
                TotalCount = total
            });
        }

        [HttpPost("webhook-inbox/{id:guid}/requeue")]
        [Authorize(Policy = AuthorizationPolicies.OpsManage)]
        public async Task<IActionResult> RequeueWebhookInbox(Guid id, CancellationToken ct)
        {
            var msg = await _db.StripeWebhookInboxMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (msg is null)
                return NotFound();

            if (msg.ProcessedOnUtc != null)
                return BadRequest("Processed messages cannot be requeued.");

            msg.NextAttemptOnUtc = DateTime.UtcNow;
            msg.LastError = null;
            await _db.SaveChangesAsync(ct);

            return NoContent();
        }
    }
}
