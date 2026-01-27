using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Recuria.Application.Contracts.Organizations;
using Recuria.Application.Interface;
using Recuria.Application.Requests;
using Recuria.Domain;
using Recuria.Domain.Enums;

namespace Recuria.Api.Controllers
{
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

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "MemberOrAbove")]
        public async Task<ActionResult<OrganizationDto>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var org = await _queries.GetByIdAsync(id, cancellationToken);

            if (org is null)
                return NotFound();

            return Ok(org);
        }

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

        [HttpPost("{id:guid}/users")]
        [Authorize(Policy = "AdminOrOwner")]
        public async Task<IActionResult> AddUser(
            Guid id,
            [FromBody] AddUserRequest request,
            CancellationToken cancellationToken)
        {
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

        [HttpPut("{orgId:guid}/users/{userId:guid}/role")]
        [Authorize(Policy = "AdminOrOwner")]
        public async Task<IActionResult> ChangeUserRole(
            Guid orgId,
            Guid userId,
            [FromBody] ChangeUserRoleRequest request,
            CancellationToken ct)
        {
            await _service.ChangeUserRoleAsync(
                orgId,
                userId,
                request.NewRole,
                ct);

            return NoContent();
        }

        [HttpDelete("{orgId:guid}/users/{userId:guid}")]
        [Authorize(Policy = "AdminOrOwner")]
        public async Task<IActionResult> RemoveUser(
            Guid orgId,
            Guid userId,
            CancellationToken ct)
        {
            await _service.RemoveUserAsync(orgId, userId, ct);
            return NoContent();
        }
    }
}
