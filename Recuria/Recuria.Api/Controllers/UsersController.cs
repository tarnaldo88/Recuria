using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
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
        private readonly IUnitOfWork _uow;

        public UsersController(IUserRepository users, IUnitOfWork uow)
        {
            _users = users;
            _uow = uow;
        }

        public sealed record CreateUserRequest(Guid Id, string Email, string? Name);

        /// <summary>
        /// Create a user.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateUserRequest request,
            CancellationToken ct)
        {
            if (request.Id == Guid.Empty)
                return BadRequest("User id is required.");

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            var name = string.IsNullOrWhiteSpace(request.Name) ? request.Email : request.Name;
            var user = new User(request.Email, name) { Id = request.Id };

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

            var orgClaim = User.FindFirst("org_id")?.Value;
            if (user.OrganizationId == null ||
                !Guid.TryParse(orgClaim, out var orgId) ||
                user.OrganizationId != orgId)
            {
                return Forbid();
            }

            return Ok(user);
        }
    }
}
