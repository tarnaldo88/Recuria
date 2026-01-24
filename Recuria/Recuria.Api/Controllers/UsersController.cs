using Microsoft.AspNetCore.Mvc;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Recuria.Api.Controllers
{
    [ApiController]
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

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<User>> GetById(Guid id, CancellationToken ct)
        {
            var user = await _users.GetByIdAsync(id, ct);
            return user is null ? NotFound() : Ok(user);
        }
    }
}
