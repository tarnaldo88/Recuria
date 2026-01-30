using Microsoft.AspNetCore.Mvc;

namespace Recuria.Api.Controllers
{
    /// <summary>
    /// Health endpoint.
    /// </summary>
    [ApiController]
    [Route("api/health")]
    public sealed class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Simple health check.
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogDebug("Health check requested.");
            return Ok(new { status = "ok" });
        }
    }
}
