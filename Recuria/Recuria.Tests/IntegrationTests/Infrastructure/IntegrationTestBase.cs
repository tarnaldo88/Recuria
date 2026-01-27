using Microsoft.IdentityModel.Tokens;
using Recuria.Domain.Enums;
using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Recuria.Tests.IntegrationTests.Infrastructure
{
    public abstract class IntegrationTestBase
    : IClassFixture<CustomWebApplicationFactory>
    {
        protected readonly HttpClient Client;
        protected static readonly JsonSerializerOptions JsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };

        protected IntegrationTestBase(CustomWebApplicationFactory factory)
        {
            Client = factory.CreateClient();
        }

        protected void SetAuthHeader(Guid userId, Guid organizationId, UserRole role)
        {
            var token = CreateJwt(userId, organizationId, role);
            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        private static string CreateJwt(Guid userId, Guid organizationId, UserRole role)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("org_id", organizationId.ToString()),
                new Claim(ClaimTypes.Role, role.ToString())
            };

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(CustomWebApplicationFactory.JwtSigningKey)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: CustomWebApplicationFactory.JwtIssuer,
                audience: CustomWebApplicationFactory.JwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
