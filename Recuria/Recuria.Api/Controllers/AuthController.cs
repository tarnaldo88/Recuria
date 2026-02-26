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
        private readonly IAuthChallengeService _challenges;
        private readonly ILogger<AuthController> _logger;
        private readonly JwtOptions _jwt;

        public AuthController(
            IUserRepository users,
            IOrganizationRepository organizations,
            IOrganizationService organizationService,
            IUnitOfWork uow,
            ITokenService tokens,
            IAuthChallengeService challenges,
            ILogger<AuthController> logger,
            Microsoft.Extensions.Options.IOptions<JwtOptions> jwt)
        {
            _users = users;
            _organizations = organizations;
            _organizationService = organizationService;
            _uow = uow;
            _tokens = tokens;
            _challenges = challenges;
            _logger = logger;
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

        public sealed class VerificationRequest
        {
            [Required]
            [StringLength(200)]
            public string OrganizationName { get; init; } = string.Empty;

            [Required]
            [EmailAddress]
            [StringLength(256)]
            public string Email { get; init; } = string.Empty;
        }

        public sealed class TokenRequest
        {
            [Required]
            [StringLength(300)]
            public string Token { get; init; } = string.Empty;
        }

        public sealed class PasswordResetRequest
        {
            [Required]
            [StringLength(300)]
            public string Token { get; init; } = string.Empty;

            [Required]
            [StringLength(256, MinimumLength = 8)]
            public string NewPassword { get; init; } = string.Empty;
        }

        public sealed record ChallengeIssuedResponse(bool Issued, string DeliveryChannel);

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

            // IMPORTANT: reload so OrganizationId/Role are populated correctly
            var reloadedOwner = await _users.GetByIdAsync(owner.Id, ct);
            if (reloadedOwner is null || reloadedOwner.OrganizationId is null)
                return Problem("Failed to finalize account provisioning.", statusCode: 500);

            var accessToken = _tokens.CreateAccessToken(reloadedOwner);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

            return Ok(new AuthResponse(
                accessToken,
                expiresAt,
                reloadedOwner.Id,
                reloadedOwner.OrganizationId.Value,
                reloadedOwner.Role.ToString(),
                reloadedOwner.Email,
                reloadedOwner.Name));
        }

        [HttpPost("request-email-verification")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [ProducesResponseType(typeof(ChallengeIssuedResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ChallengeIssuedResponse>> RequestEmailVerification([FromBody] VerificationRequest request, CancellationToken ct)
        {
            var user = await ResolveUserAsync(request.OrganizationName, request.Email, ct);
            if (user is not null)
            {
                var token = _challenges.IssueEmailVerification(user.Id, TimeSpan.FromHours(24));
                _logger.LogInformation("Email verification token generated for {Email}: {Token}", user.Email, token);
            }
            return Ok(new ChallengeIssuedResponse(true, "email"));
        }

        [HttpPost("verify-email")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmail([FromBody] TokenRequest request, CancellationToken ct)
        {
            if (!_challenges.TryConsumeEmailVerification(request.Token, out var userId))
                return BadRequest("Invalid or expired verification token.");

            var user = await _users.GetByIdAsync(userId, ct);
            if (user is null)
                return BadRequest("User not found.");

            // NOTE: no persisted flag yet; token consumption is the current proof-of-verification.
            _logger.LogInformation("Email verification completed for user {UserId}", userId);
            return NoContent();
        }

        [HttpPost("request-password-reset")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [ProducesResponseType(typeof(ChallengeIssuedResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ChallengeIssuedResponse>> RequestPasswordReset([FromBody] VerificationRequest request, CancellationToken ct)
        {
            var user = await ResolveUserAsync(request.OrganizationName, request.Email, ct);
            if (user is not null)
            {
                var token = _challenges.IssuePasswordReset(user.Id, TimeSpan.FromHours(2));
                _logger.LogInformation("Password reset token generated for {Email}: {Token}", user.Email, token);
            }
            return Ok(new ChallengeIssuedResponse(true, "email"));
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetRequest request, CancellationToken ct)
        {
            if (!_challenges.TryConsumePasswordReset(request.Token, out var userId))
                return BadRequest("Invalid or expired reset token.");

            var user = await _users.GetByIdAsync(userId, ct);
            if (user is null)
                return BadRequest("User not found.");

            user.SetPassword(request.NewPassword);
            user.RotateTokenVersion();
            _users.Update(user);
            await _uow.CommitAsync(ct);
            return NoContent();
        }

        [HttpPost("request-magic-link")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [ProducesResponseType(typeof(ChallengeIssuedResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ChallengeIssuedResponse>> RequestMagicLink([FromBody] VerificationRequest request, CancellationToken ct)
        {
            var user = await ResolveUserAsync(request.OrganizationName, request.Email, ct);
            if (user is not null)
            {
                var token = _challenges.IssueMagicLink(user.Id, TimeSpan.FromMinutes(20));
                _logger.LogInformation("Magic link token generated for {Email}: {Token}", user.Email, token);
            }
            return Ok(new ChallengeIssuedResponse(true, "email"));
        }

        [HttpPost("magic-link-login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthResponse>> MagicLinkLogin([FromBody] TokenRequest request, CancellationToken ct)
        {
            if (!_challenges.TryConsumeMagicLink(request.Token, out var userId))
                return BadRequest("Invalid or expired magic-link token.");

            var user = await _users.GetByIdAsync(userId, ct);
            if (user is null || user.OrganizationId is null)
                return BadRequest("User not found.");

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

        private async Task<Domain.User?> ResolveUserAsync(string organizationName, string email, CancellationToken ct)
        {
            var org = await _organizations.GetByNameAsync(organizationName.Trim(), ct);
            if (org is null)
                return null;

            var normalizedEmail = email.Trim().ToLowerInvariant();
            return await _users.GetByEmailInOrganizationAsync(org.Id, normalizedEmail, ct);
        }
    }
}
