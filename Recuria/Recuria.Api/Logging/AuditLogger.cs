using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Recuria.Api.Middleware;

namespace Recuria.Api.Logging
{
    public sealed class AuditLogger : IAuditLogger
    {
        private readonly ILogger<AuditLogger> _logger;

        public AuditLogger(ILogger<AuditLogger> logger)
        {
            _logger = logger;
        }

        public void Log(HttpContext context, string action, object details)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");
            var orgId = context.User.FindFirstValue("org_id");
            var role = context.User.FindFirstValue(ClaimTypes.Role);
            var correlationId = context.Response.Headers.TryGetValue(
                CorrelationIdMiddleware.HeaderName, out var value)
                ? value.ToString()
                : null;

            _logger.LogInformation(
                "audit {Action} userId={UserId} orgId={OrgId} role={Role} correlationId={CorrelationId} details={Details}",
                action,
                userId,
                orgId,
                role,
                correlationId,
                JsonSerializer.Serialize(details));
        }
    }
}
