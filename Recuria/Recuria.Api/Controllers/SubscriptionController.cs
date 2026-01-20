using Microsoft.AspNetCore.Mvc;
using Recuria.Application.Contracts.Subscription;
using Recuria.Application.Interface;
using Recuria.Application.Requests;

namespace Recuria.Api.Controllers
{
    [ApiController]
    [Route("api/subscriptions")]
    public class SubscriptionController : ControllerBase
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
            return subscription is null ? NotFound() : Ok(subscription);
        }

        [HttpPost("trial/{organizationId:guid}")]
        public async Task<ActionResult<SubscriptionDto>> CreateTrial(Guid organizationId, CancellationToken ct)
        {
            var dto = await _subscriptionService.CreateTrialAsync(organizationId, ct);

            return CreatedAtAction(nameof(GetCurrent), new { organizationId }, dto);
        }

        [HttpPost("{subscriptionId:guid}/upgrade")]
        public async Task<IActionResult> Upgrade(Guid subscriptionId, [FromBody] UpgradeSubscriptionRequest request, CancellationToken ct)
        {
            var subscription = await _subscriptionQueries.GetDomainByIdAsync(subscriptionId);
            await _subscriptionService.UpgradeAsync(subscriptionId, request.NewPlan, ct);
            return NoContent();
        }

        [HttpPost("{subscriptionId:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid subscriptionId)
        {
            var subscription = await _subscriptionQueries.GetDomainByIdAsync(subscriptionId);
            CancellationToken ct = new CancellationToken();
            await _subscriptionService.CancelAsync(subscriptionId, ct);
            return NoContent();
        }
    }
}
