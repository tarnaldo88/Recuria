using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Requests;
using Recuria.Domain;
using Recuria.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Recuria.Api.Controllers
{
    [ApiController]
    [Route("api/dev/bootstrap")]
    [AllowAnonymous]
    public sealed class DevBootstrapController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly IUserRepository _users;
        private readonly IOrganizationService _organizations;
        private readonly IUnitOfWork _uow;

        public DevBootstrapController(
            IWebHostEnvironment env,
            IConfiguration config,
            IUserRepository users,
            IOrganizationService organizations,
            IUnitOfWork uow)
        {
            _env = env;
            _config = config;
            _users = users;
            _organizations = organizations;
            _uow = uow;
        }

        public sealed record BootstrapRequest(
            string OrganizationName,
            string OwnerEmail,
            string? OwnerName);

        public sealed record BootstrapResponse(
            Guid OwnerUserId,
            Guid OrganizationId,
            string Token);

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

            await _users.AddAsync(owner, ct);
            await _uow.CommitAsync(ct);

            var orgId = await _organizations.CreateOrganizationAsync(
                new CreateOrganizationRequest
                {
                    Name = request.OrganizationName,
                    OwnerId = ownerId
                },
                ct);

            var token = CreateJwt(ownerId, orgId, UserRole.Owner);

            return Ok(new BootstrapResponse(ownerId, orgId, token));
        }

        private string CreateJwt(Guid userId, Guid organizationId, UserRole role)
        {
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            var key = _config["Jwt:SigningKey"];

            if (string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audience) ||
                string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("JWT configuration is missing.");
            }

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("org_id", organizationId.ToString()),
                new Claim(ClaimTypes.Role, role.ToString())
            };

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
