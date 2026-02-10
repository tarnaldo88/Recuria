using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recuria.Api.Auth;
using Recuria.Api.Configuration;
using Recuria.Application.Interface.Abstractions;
using System.Security.Claims;

namespace Recuria.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _uow;
        private readonly ITokenService _tokens;
        private readonly JwtOptions _jwt;

        public AuthController(
            IUserRepository users,
            IUnitOfWork uow,
            ITokenService tokens,
            Microsoft.Extensions.Options.IOptions<JwtOptions> jwt)
        {
            _users = users;
            _uow = uow;
            _tokens = tokens;
            _jwt = jwt.Value;
        }

        public sealed record LoginRequest(Guid OrganizationId, string Email, string Password);

        public sealed record AuthResponse(
            string AccessToken,
            DateTime ExpiresAtUtc,
            Guid UserId,
            Guid OrganizationId,
            string Role,
            string Email,
            string Name);

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            if (request.OrganizationId == Guid.Empty)
                return BadRequest("OrganizationId is required.");

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email and password are required.");

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _users.GetByEmailInOrganizationAsync(request.OrganizationId, normalizedEmail, ct);
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
