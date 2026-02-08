using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recuria.Application.Contracts.Subscription;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Requests;
using Recuria.Api.Logging;
using Recuria.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace Recuria.Api.Controllers
{
    /// <summary>
    /// Subscription management endpoints.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/subscriptions")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ISubscriptionQueries _subscriptionQueries;
        private readonly ISubscriptionRepository _subscriptions;
        private readonly IAuditLogger _audit;
        private readonly IMemoryCache _cache;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            ISubscriptionQueries subscriptionQueries,
            ISubscriptionRepository subscriptions,
            IAuditLogger audit,
            IMemoryCache cache)
        {
            _subscriptionService = subscriptionService;
            _subscriptionQueries = subscriptionQueries;
            _subscriptions = subscriptions;
            _audit = audit;
            _cache = cache;
        }

        /// <summary>
        /// Get the current subscription for an organization.
        /// </summary>
        [HttpGet("current/{organizationId:guid}")]
        [Authorize(Policy = "MemberOrAbove")]
        public async Task<ActionResult<SubscriptionDetailsDto>> GetCurrent(Guid organizationId, CancellationToken ct)
        {
            if (!IsSameOrganization(organizationId))
                return Forbid();

            var cacheKey = $"sub:current:{organizationId}";
            if (!_cache.TryGetValue(cacheKey, out SubscriptionDetailsDto? subscription))
            {
                subscription = await _subscriptionQueries.GetCurrentAsync(organizationId, ct);
                if (subscription != null)
                {
                    _cache.Set(cacheKey, subscription, TimeSpan.FromSeconds(30));
                }
            }
            return subscription is null ? NotFound() : Ok(subscription);
        }

        /// <summary>
        /// Create a trial subscription for an organization.
        /// </summary>
        [HttpPost("trial/{organizationId:guid}")]
        [Authorize(Policy = "AdminOrOwner")]
        public async Task<ActionResult<SubscriptionDto>> CreateTrial(Guid organizationId, CancellationToken ct)
        {
            if (!IsSameOrganization(organizationId))
                return Forbid();

            var dto = await _subscriptionService.CreateTrialAsync(organizationId, ct);

            _audit.Log(HttpContext, "subscription.trial.create", new
            {
                organizationId
            });

            return CreatedAtAction(nameof(GetCurrent), new { organizationId }, dto);
        }

        /// <summary>
        /// Upgrade a subscription plan.
        /// </summary>
        [HttpPost("{subscriptionId:guid}/upgrade")]
        [Authorize(Policy = "AdminOrOwner")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

            _audit.Log(HttpContext, "subscription.upgrade", new
            {
                subscriptionId,
                newPlan = request.NewPlan
            });
            _cache.Remove($"sub:current:{subscription.OrganizationId}");
            return NoContent();
        }

        /// <summary>
        /// Cancel a subscription.
        /// </summary>
        [HttpPost("{subscriptionId:guid}/cancel")]
        [Authorize(Policy = "AdminOrOwner")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(Guid subscriptionId, CancellationToken ct)
        {
            var subscription = await _subscriptions.GetByIdAsync(subscriptionId, ct);
            if (subscription == null)
                return NotFound();
            if (!IsSameOrganization(subscription.OrganizationId))
                return Forbid();

            await _subscriptionService.CancelAsync(subscriptionId, ct);

            _audit.Log(HttpContext, "subscription.cancel", new
            {
                subscriptionId
            });
            _cache.Remove($"sub:current:{subscription.OrganizationId}");
            return NoContent();
        }

        private bool IsSameOrganization(Guid organizationId)
        {
            var orgClaim = User.FindFirst("org_id")?.Value;
            return Guid.TryParse(orgClaim, out var orgId) && orgId == organizationId;
        }
    }
}
