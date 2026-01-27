using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Recuria.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/auth")]
    public sealed class WhoAmIController : ControllerBase
    {
        public sealed record WhoAmIResponse(
            string? Subject,
            string? OrganizationId,
            string? Role,
            IEnumerable<KeyValuePair<string, string>> Claims);

        [HttpGet("whoami")]
        public ActionResult<WhoAmIResponse> WhoAmI()
        {
            var claims = User.Claims
                .Select(c => new KeyValuePair<string, string>(c.Type, c.Value))
                .ToList();

            return Ok(new WhoAmIResponse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub"),
                User.FindFirstValue("org_id"),
                User.FindFirstValue(ClaimTypes.Role),
                claims));
        }
    }
}
