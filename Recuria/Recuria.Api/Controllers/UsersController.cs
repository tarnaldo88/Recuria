using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recuria.Api.Auth;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using Recuria.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Recuria.Api.Controllers
{
    /// <summary>
    /// User management endpoints.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "AdminOrOwner")]
    [Route("api/users")]
    public sealed class UsersController : ControllerBase
    {
        private readonly IUserRepository _users;
        private readonly IOrganizationRepository _organizations;
        private readonly IUnitOfWork _uow;

        public UsersController(
            IUserRepository users,
            IOrganizationRepository organizations,
            IUnitOfWork uow)
        {
            _users = users;
            _organizations = organizations;
            _uow = uow;
        }

        /// <summary>
        /// Request to create a user.
        /// </summary>
        public sealed record CreateUserRequest(
            Guid Id,
            Guid OrganizationId,
            string Email,
            string? Name,
            string? Password);

        /// <summary>
        /// Create a user.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateUserRequest request,
            CancellationToken ct)
        {
            if (!User.IsInOrganization(request.OrganizationId))
                return Forbid();

            if (request.Id == Guid.Empty)
                return BadRequest("User id is required.");

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            var organization = await _organizations.GetByIdAsync(request.OrganizationId, ct);
            if (organization is null)
                return BadRequest("Organization does not exist.");

            var name = string.IsNullOrWhiteSpace(request.Name) ? request.Email : request.Name;
            var user = new User(request.Email, name) { Id = request.Id };
            user.AssignToOrganization(organization, UserRole.Member);
            if (!string.IsNullOrWhiteSpace(request.Password))
                user.SetPassword(request.Password);

            await _users.AddAsync(user, ct);
            await _uow.CommitAsync(ct);

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user.Id);
        }

        /// <summary>
        /// Get a user by id (must match org_id claim).
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<User>> GetById(Guid id, CancellationToken ct)
        {
            var user = await _users.GetByIdAsync(id, ct);
            if (user is null)
                return NotFound();

            if (user.OrganizationId == null || !User.IsInOrganization(user.OrganizationId.Value))
            {
                return Forbid();
            }

            return Ok(user);
        }
    }
}
