using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recuria.Api.Auth;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Requests;
using Recuria.Domain;
using Recuria.Domain.Enums;

namespace Recuria.Api.Controllers
{
    /// <summary>
    /// Development-only bootstrap endpoint to seed an org and return a JWT.
    /// </summary>
    [ApiController]
    [Route("api/dev/bootstrap")]
    [AllowAnonymous]
    public sealed class DevBootstrapController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IUserRepository _users;
        private readonly IOrganizationService _organizations;
        private readonly IUnitOfWork _uow;
        private readonly ITokenService _tokens;

        public DevBootstrapController(
            IWebHostEnvironment env,
            IUserRepository users,
            IOrganizationService organizations,
            IUnitOfWork uow,
            ITokenService tokens)
        {
            _env = env;
            _users = users;
            _organizations = organizations;
            _uow = uow;
            _tokens = tokens;
        }

        /// <summary>
        /// Input for the dev bootstrap endpoint.
        /// </summary>
        public sealed record BootstrapRequest(
            string OrganizationName,
            string OwnerEmail,
            string? OwnerName,
            string? OwnerPassword);

        /// <summary>
        /// Response payload for the dev bootstrap endpoint.
        /// </summary>
        public sealed record BootstrapResponse(
            Guid OwnerUserId,
            Guid OrganizationId,
            string Token);

        /// <summary>
        /// Creates an owner user + organization and returns a JWT with org_id and role claims.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BootstrapResponse>> Bootstrap(
            [FromBody] BootstrapRequest request,
            CancellationToken ct)
        {
            if (!_env.IsDevelopment())
                return NotFound();

            if (string.IsNullOrWhiteSpace(request.OrganizationName))
                return BadRequest("OrganizationName is required.");

            if (string.IsNullOrWhiteSpace(request.OwnerEmail))
                return BadRequest("OwnerEmail is required.");

            var ownerId = Guid.NewGuid();
            var ownerName = string.IsNullOrWhiteSpace(request.OwnerName)
                ? request.OwnerEmail
                : request.OwnerName;

            var owner = new User(request.OwnerEmail, ownerName) { Id = ownerId };
            if (!string.IsNullOrWhiteSpace(request.OwnerPassword))
                owner.SetPassword(request.OwnerPassword);

            await _users.AddAsync(owner, ct);
            await _uow.CommitAsync(ct);

            var orgId = await _organizations.CreateOrganizationAsync(
                new CreateOrganizationRequest
                {
                    Name = request.OrganizationName,
                    OwnerId = ownerId
                },
                ct);

            var reloadedOwner = await _users.GetByIdAsync(ownerId, ct) ?? owner;
            var token = _tokens.CreateAccessToken(reloadedOwner);

            return Ok(new BootstrapResponse(ownerId, orgId, token));
        }
    }
}
