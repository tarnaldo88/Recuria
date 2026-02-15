using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Recuria.Api.Auth;
using Recuria.Application.Contracts.Organizations;
using Recuria.Application.Interface;
using Recuria.Application.Requests;
using Recuria.Application.Contracts.Common;
using Recuria.Api.Logging;
using Recuria.Domain;
using Recuria.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace Recuria.Api.Controllers
{
    /// <summary>
    /// Organization management endpoints.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/organizations")]
    public class OrganizationController : ControllerBase
    {
        private readonly IOrganizationQueries _queries;
        private readonly IOrganizationService _service;
        private readonly ILogger<OrganizationController> _logger;
        private readonly IAuditLogger _audit;
        private readonly IMemoryCache _cache;

        public OrganizationController(
            IOrganizationQueries queries,
            IOrganizationService service,
            ILogger<OrganizationController> logger,
            IAuditLogger audit,
            IMemoryCache cache)
        {
            _queries = queries;
            _service = service;
            _logger = logger;
            _audit = audit;
            _cache = cache;
        }

        /// <summary>
        /// Create a new organization.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "MemberOrAbove")]
        public async Task<ActionResult<OrganizationDto>> Create(
            [FromBody] CreateOrganizationRequest request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating organization {Name}", request.Name);

            var orgId = await _service.CreateOrganizationAsync(request, cancellationToken);

            var org = await _queries.GetByIdAsync(orgId, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = orgId },
                org);
        }

        /// <summary>
        /// Get an organization by id (must match org_id claim).
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = "MemberOrAbove")]
        public async Task<ActionResult<OrganizationDto>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            if (!IsSameOrganization(id))
                return Forbid();

            var cacheKey = $"org:{id}";
            if (!_cache.TryGetValue(cacheKey, out OrganizationDto? org))
            {
                org = await _queries.GetByIdAsync(id, cancellationToken);
                if (org != null)
                {
                    _cache.Set(cacheKey, org, TimeSpan.FromSeconds(30));
                }
            }

            if (org is null)
                return NotFound();

            return Ok(org);
        }

        /// <summary>
        /// Get the organization from the current JWT (org_id claim).
        /// </summary>
        [HttpGet("me")]
        [Authorize(Policy = "MemberOrAbove")]
        public async Task<ActionResult<OrganizationDto>> GetMyOrganization(
            CancellationToken cancellationToken)
        {
            if (!User.TryGetOrganizationId(out var organizationId))
                return Forbid();

            var cacheKey = $"org:{organizationId}";
            if (!_cache.TryGetValue(cacheKey, out OrganizationDto? org))
            {
                org = await _queries.GetByIdAsync(organizationId, cancellationToken);
                if (org != null)
                {
                    _cache.Set(cacheKey, org, TimeSpan.FromSeconds(30));
                }
            }

            return org is null
                ? NotFound()
                : Ok(org);
        }

        /// <summary>
        /// Add a user to the organization (Admin/Owner only).
        /// </summary>
        [HttpPost("{id:guid}/users")]
        [Authorize(Policy = "AdminOrOwner")]
        public async Task<IActionResult> AddUser(
            Guid id,
            [FromBody] AddUserRequest request,
            CancellationToken cancellationToken)
        {
            if (!IsSameOrganization(id))
                return Forbid();

            await _service.AddUserAsync(id, request, cancellationToken);

            _audit.Log(HttpContext, "org.user.add", new
            {
                organizationId = id,
                request.UserId,
                request.Role
            });
            _cache.Remove($"org:{id}");

            return NoContent();
        }

        /// <summary>
        /// Change a user's role in the organization (Admin/Owner only).
        /// </summary>
        [HttpPut("{orgId:guid}/users/{userId:guid}/role")]
        [Authorize(Policy = "AdminOrOwner")]
        public async Task<IActionResult> ChangeUserRole(
            Guid orgId,
            Guid userId,
            [FromBody] ChangeUserRoleRequest request,
            CancellationToken ct)
        {
            if (!IsSameOrganization(orgId))
                return Forbid();

            await _service.ChangeUserRoleAsync(
                orgId,
                userId,
                request.NewRole,
                ct);

            _audit.Log(HttpContext, "org.user.role_change", new
            {
                organizationId = orgId,
                userId,
                newRole = request.NewRole
            });
            _cache.Remove($"org:{orgId}");

            return NoContent();
        }

        /// <summary>
        /// Remove a user from the organization (Admin/Owner only).
        /// </summary>
        [HttpDelete("{orgId:guid}/users/{userId:guid}")]
        [Authorize(Policy = "AdminOrOwner")]
        public async Task<IActionResult> RemoveUser(
            Guid orgId,
            Guid userId,
            CancellationToken ct)
        {
            if (!IsSameOrganization(orgId))
                return Forbid();

            await _service.RemoveUserAsync(orgId, userId, ct);

            _audit.Log(HttpContext, "org.user.remove", new
            {
                organizationId = orgId,
                userId
            });
            _cache.Remove($"org:{orgId}");

            return NoContent();
        }

        /// <summary>
        /// Get users from the same organization.
        /// </summary>
        [HttpGet("{id:guid}/users")]
        [Authorize(Policy = "AdminOrOwner")]
        public async Task<ActionResult<PagedResult<UserSummaryDto>>> GetUsers(
            Guid id,
            [FromQuery] TableQuery query,
            CancellationToken ct)
        {
            if (!IsSameOrganization(id))
                return Forbid();

            var safe = new TableQuery
            {
                Page = Math.Max(1, query.Page),
                PageSize = Math.Clamp(query.PageSize, 5, 100),
                Search = query.Search,
                SortBy = query.SortBy,
                SortDir = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc"
            };

            var users = await _queries.GetUsersPagedAsync(id, safe, ct);
            return Ok(users);
        }

        private bool IsSameOrganization(Guid organizationId)
        {
            return User.IsInOrganization(organizationId);
        }

    }
}
