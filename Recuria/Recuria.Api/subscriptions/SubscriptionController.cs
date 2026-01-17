using Microsoft.AspNetCore.Mvc;
using Recuria.Application.Contracts.Subscription;
using Recuria.Application.Interface;
using Recuria.Application.Requests;

namespace Recuria.Api.subscriptions
{
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ISubscriptionQueries _subscriptionQueries;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            ISubscriptionQueries subscriptionQueries)
        {
            _subscriptionService = subscriptionService;
            _subscriptionQueries = subscriptionQueries;
        }

        [HttpGet("current/{organizationId:guid}")]
        public async Task<ActionResult<SubscriptionDetailsDto>> GetCurrent(Guid organizationId, CancellationToken ct)
        {
            var subscription = await _subscriptionQueries.GetCurrentAsync(organizationId, ct);
            if (subscription is null) return NotFound();
            return Ok(subscription);
        }

        [HttpPost("trial/{organizationId:guid}")]
        public async Task<ActionResult<SubscriptionDto>> CreateTrial(Guid organizationId)
        {
            var org = await _subscriptionQueries.GetDomainAsync(organizationId);
            var subscription = _subscriptionService.CreateTrial(org);

            return CreatedAtAction(nameof(GetCurrent), new { organizationId }, subscription);
        }

        [HttpPost("{subscriptionId:guid}/upgrade")]
        public async Task<IActionResult> Upgrade(Guid subscriptionId, [FromBody] UpgradeSubscriptionRequest request)
        {
            var subscription = await _subscriptionQueries.GetDomainByIdAsync(subscriptionId);
            _subscriptionService.UpgradePlan(subscription, request.NewPlan);
            return NoContent();
        }

        [HttpPost("{subscriptionId:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid subscriptionId)
        {
            var subscription = await _subscriptionQueries.GetDomainByIdAsync(subscriptionId);
            CancellationToken ct = new CancellationToken();
            _subscriptionService.CancelSubscription(subscription, ct);
            return NoContent();
        }
    }
}
