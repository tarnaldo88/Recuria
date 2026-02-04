using Microsoft.AspNetCore.Http;

namespace Recuria.Api.Logging
{
    public interface IAuditLogger
    {
        void Log(HttpContext context, string action, object details);
    }
}
