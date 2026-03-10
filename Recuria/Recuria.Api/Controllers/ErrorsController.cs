using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recuria.Application.Observability;

namespace Recuria.Api.Controllers
{
    /// <summary>
    /// Error tracking and monitoring endpoint.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "Admin,Owner")]
    [Route("api/errors")]
    public class ErrorsController : ControllerBase
    {
        private readonly IErrorTrackingService _errorService;

        public ErrorsController(IErrorTrackingService errorService)
        {
            _errorService = errorService;
        }

        /// <summary>
        /// Get recent errors.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRecent([FromQuery] int count = 50)
        {
            var errors = await _errorService.GetRecentErrorsAsync(count);
            return Ok(errors);
        }

        /// <summary>
        /// Get error summary.
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] int hours = 24)
        {
            var summary = await _errorService.GetErrorSummaryAsync(TimeSpan.FromHours(hours));
            return Ok(summary);
        }

        /// <summary>
        /// Clear all errors.
        /// </summary>
        [HttpDelete]
        public IActionResult Clear()
        {
            _errorService.ClearErrors();
            return NoContent();
        }
    }
}