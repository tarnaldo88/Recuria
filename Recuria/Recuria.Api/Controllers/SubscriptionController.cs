using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recuria.Application.Contracts.Subscription;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Requests;
using Recuria.Domain.Enums;

namespace Recuria.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/subscriptions")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ISubscriptionQueries _subscriptionQueries;
        private readonly ISubscriptionRepository _subscriptions;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            ISubscriptionQueries subscriptionQueries,
            ISubscriptionRepository subscriptions)
        {
            _subscriptionService = subscriptionService;
            _subscriptionQueries = subscriptionQueries;
            _subscriptions = subscriptions;
        }

        [HttpGet("current/{organizationId:guid}")]
        [Authorize(Policy = "MemberOrAbove")]
        public async Task<ActionResult<SubscriptionDetailsDto>> GetCurrent(Guid organizationId, CancellationToken ct)
        {
            if (!IsSameOrganization(organizationId))
                return Forbid();

            var subscription = await _subscriptionQueries.GetCurrentAsync(organizationId, ct);
            return subscription is null ? NotFound() : Ok(subscription);
        }

        [HttpPost("trial/{organizationId:guid}")]
        [Authorize(Policy = "AdminOrOwner")]
        public async Task<ActionResult<SubscriptionDto>> CreateTrial(Guid organizationId, CancellationToken ct)
        {
            if (!IsSameOrganization(organizationId))
                return Forbid();

            var dto = await _subscriptionService.CreateTrialAsync(organizationId, ct);

            return CreatedAtAction(nameof(GetCurrent), new { organizationId }, dto);
        }

        [HttpPost("{subscriptionId:guid}/upgrade")]
        [Authorize(Policy = "AdminOrOwner")]
        public async Task<IActionResult> Upgrade(
            Guid subscriptionId,
            [FromBody] UpgradeSubscriptionRequest request,
            CancellationToken ct)
        {
            var subscription = await _subscriptions.GetByIdAsync(subscriptionId, ct);
            if (subscription == null)
                return NotFound();
            if (!IsSameOrganization(subscription.OrganizationId))
                return Forbid();

            await _subscriptionService.UpgradeAsync(subscriptionId, request.NewPlan, ct);
            return NoContent();
        }

        [HttpPost("{subscriptionId:guid}/cancel")]
        [Authorize(Policy = "AdminOrOwner")]
        public async Task<IActionResult> Cancel(Guid subscriptionId, CancellationToken ct)
        {
            var subscription = await _subscriptions.GetByIdAsync(subscriptionId, ct);
            if (subscription == null)
                return NotFound();
            if (!IsSameOrganization(subscription.OrganizationId))
                return Forbid();

            await _subscriptionService.CancelAsync(subscriptionId, ct);
            return NoContent();
        }

        private bool IsSameOrganization(Guid organizationId)
        {
            var orgClaim = User.FindFirst("org_id")?.Value;
            return Guid.TryParse(orgClaim, out var orgId) && orgId == organizationId;
        }
    }
}
