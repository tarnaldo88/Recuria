using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Recuria.Application.Contracts.Organizations;
using Recuria.Application.Interface;
using Recuria.Application.Requests;
using Recuria.Domain;
using Recuria.Domain.Enums;

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

        public OrganizationController(
            IOrganizationQueries queries,
            IOrganizationService service,
            ILogger<OrganizationController> logger)
        {
            _queries = queries;
            _service = service;
            _logger = logger;
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

            var org = await _queries.GetByIdAsync(id, cancellationToken);

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
            var organizationId = GetOrganizationIdFromContext();

            var org = await _queries.GetByIdAsync(organizationId, cancellationToken);

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

            return NoContent();
        }

        // Temporary until auth exists
        private Guid GetOrganizationIdFromContext()
        {
            // In next step replaced by auth claims
            return Guid.Parse(
                User.FindFirst("org_id")?.Value
                ?? throw new InvalidOperationException("No organization in context"));
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
            return NoContent();
        }

        private bool IsSameOrganization(Guid organizationId)
        {
            var orgClaim = User.FindFirst("org_id")?.Value;
            return Guid.TryParse(orgClaim, out var orgId) && orgId == organizationId;
        }
    }
}
