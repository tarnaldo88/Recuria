using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recuria.Application.Interface;
using Recuria.Domain;
using System.Security.Claims;

namespace Recuria.Api.Controllers
{
    /// <summary>
    /// Feature flags management endpoint.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "Admin,Owner")]
    [Route("api/feature-flags")]
    public class FeatureFlagsController : ControllerBase
    {
        private readonly IFeatureFlagService _featureFlagService;
        private readonly ILogger<FeatureFlagsController> _logger;

        public FeatureFlagsController(
            IFeatureFlagService featureFlagService,
            ILogger<FeatureFlagsController> logger)
        {
            _featureFlagService = featureFlagService;
            _logger = logger;
        }

        /// <summary>
        /// Get all feature flags.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var flags = await _featureFlagService.GetAllAsync();
            return Ok(flags);
        }

        /// <summary>
        /// Check if a feature is enabled.
        /// </summary>
        [HttpGet("check/{name}")]
        [AllowAnonymous]
        public async Task<IActionResult> Check(string name)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var orgId = User.FindFirst("org_id")?.Value;
            
            var isEnabled = await _featureFlagService.IsEnabledAsync(name, userEmail, orgId);
            return Ok(new { name, isEnabled });
        }

        /// <summary>
        /// Create a new feature flag.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FeatureFlag flag)
        {
            await _featureFlagService.CreateAsync(flag);
            return CreatedAtAction(nameof(GetAll), new { id = flag.Id }, flag);
        }

        /// <summary>
        /// Update a feature flag.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] FeatureFlag flag)
        {
            flag.Id = id;
            await _featureFlagService.UpdateAsync(flag);
            return NoContent();
        }

        /// <summary>
        /// Toggle a feature flag.
        /// </summary>
        [HttpPost("{name}/toggle")]
        public async Task<IActionResult> Toggle(string name, [FromBody] ToggleRequest request)
        {
            var modifiedBy = User.Identity?.Name ?? "unknown";
            await _featureFlagService.ToggleAsync(name, request.Enabled, modifiedBy);
            return Ok(new { name, enabled = request.Enabled });
        }

        /// <summary>
        /// Delete a feature flag.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _featureFlagService.DeleteAsync(id);
            return NoContent();
        }

        public class ToggleRequest
        {
            public bool Enabled { get; set; }
        }
    }
}