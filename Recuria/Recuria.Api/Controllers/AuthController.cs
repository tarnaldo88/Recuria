using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Recuria.Api.Auth;
using Recuria.Api.Configuration;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Requests;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace Recuria.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IUserRepository _users;
        private readonly IOrganizationRepository _organizations;
        private readonly IOrganizationService _organizationService;
        private readonly IUnitOfWork _uow;
        private readonly ITokenService _tokens;
        private readonly JwtOptions _jwt;

        public AuthController(
            IUserRepository users,
            IOrganizationRepository organizations,
            IOrganizationService organizationService,
            IUnitOfWork uow,
            ITokenService tokens,
            Microsoft.Extensions.Options.IOptions<JwtOptions> jwt)
        {
            _users = users;
            _organizations = organizations;
            _organizationService = organizationService;
            _uow = uow;
            _tokens = tokens;
            _jwt = jwt.Value;
        }

        public sealed class LoginRequest
        {
            public Guid? OrganizationId { get; init; }

            [StringLength(200)]
            public string? OrganizationName { get; init; }

            [Required]
            [EmailAddress]
            [StringLength(256)]
            public string Email { get; init; } = string.Empty;

            [Required]
            [StringLength(256, MinimumLength = 8)]
            public string Password { get; init; } = string.Empty;
        }

        public sealed record AuthResponse(
            string AccessToken,
            DateTime ExpiresAtUtc,
            Guid UserId,
            Guid OrganizationId,
            string Role,
            string Email,
            string Name);

        public sealed class RegisterRequest
        {
            [Required]
            [StringLength(200, MinimumLength = 2)]
            public string OrganizationName { get; init; } = string.Empty;

            [Required]
            [StringLength(120, MinimumLength = 2)]
            public string OwnerName { get; init; } = string.Empty;

            [Required]
            [EmailAddress]
            [StringLength(256)]
            public string Email { get; init; } = string.Empty;

            [Required]
            [StringLength(256, MinimumLength = 8)]
            public string Password { get; init; } = string.Empty;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            Guid organizationId;
            if (!string.IsNullOrWhiteSpace(request.OrganizationName))
            {
                var organization = await _organizations.GetByNameAsync(request.OrganizationName, ct);
                if (organization is null)
                    return Unauthorized();

                organizationId = organization.Id;
            }
            else if (request.OrganizationId.HasValue && request.OrganizationId.Value != Guid.Empty)
            {
                organizationId = request.OrganizationId.Value;
            }
            else
            {
                return BadRequest("OrganizationName (or OrganizationId) is required.");
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _users.GetByEmailInOrganizationAsync(organizationId, normalizedEmail, ct);
            if (user is null || !user.VerifyPassword(request.Password))
                return Unauthorized();

            var accessToken = _tokens.CreateAccessToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

            return Ok(new AuthResponse(
                accessToken,
                expiresAt,
                user.Id,
                user.OrganizationId!.Value,
                user.Role.ToString(),
                user.Email,
                user.Name));
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            var orgName = request.OrganizationName.Trim();
            var existing = await _organizations.GetByNameAsync(orgName, ct);
            if (existing is not null)
                return Conflict("Organization name is already taken.");

            var owner = new Domain.User(request.Email.Trim(), request.OwnerName.Trim());
            owner.SetPassword(request.Password);

            await _users.AddAsync(owner, ct);
            await _uow.CommitAsync(ct);

            var orgId = await _organizationService.CreateOrganizationAsync(
                new CreateOrganizationRequest
                {
                    Name = orgName,
                    OwnerId = owner.Id
                },
                ct);

            var accessToken = _tokens.CreateAccessToken(owner);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

            return Ok(new AuthResponse(
                accessToken,
                expiresAt,
                owner.Id,
                orgId,
                owner.Role.ToString(),
                owner.Email,
                owner.Name));
        }

        [HttpPost("refresh")]
        [Authorize]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _users.GetByIdAsync(userId, ct);
            if (user is null || user.OrganizationId is null)
                return Unauthorized();

            var accessToken = _tokens.CreateAccessToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

            return Ok(new AuthResponse(
                accessToken,
                expiresAt,
                user.Id,
                user.OrganizationId.Value,
                user.Role.ToString(),
                user.Email,
                user.Name));
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _users.GetByIdAsync(userId, ct);
            if (user is null)
                return Unauthorized();

            user.RotateTokenVersion();
            _users.Update(user);
            await _uow.CommitAsync(ct);

            return NoContent();
        }
    }
}
