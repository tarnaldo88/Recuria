using Microsoft.AspNetCore.Mvc;
using Recuria.Application.Contracts.Organizations;
using Recuria.Application.Interface;
using Recuria.Domain;
using Recuria.Infrastructure.Persistence.Queries.Interface;

namespace Recuria.Api.organizations
{
    public class OrganizationController : Controller
    {
        private readonly IOrganizationService _organizationService;
        private readonly IOrganizationQueries _organizationQueries;

        public OrganizationController(IOrganizationService orgService, IOrganizationQueries orgQueries)
        {
            _organizationService = orgService;
            _organizationQueries = orgQueries;
        }

        [HttpPost]
        public async Task<ActionResult<OrganizationDto>> CreateOrganization([FromBody] CreateOrganizationRequest request)
        {
            var owner = new User(request.OwnerName, request.OwnerEmail);
            var org = _organizationService.CreateOrganization(request.Name, owner);

            var dto = new OrganizationDto(org.Id, org.Name, org.CreatedAt);
            return CreatedAtAction(nameof(GetOrganization), new { id = org.Id }, dto);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<OrganizationDto>> GetOrganization(Guid id)
        {
            var org = await _organizationQueries.GetAsync(id);
            if (org is null) return NotFound();
            return Ok(org);
        }

        [HttpPost("{id:guid}/users")]
        public async Task<IActionResult> AddUser(Guid id, [FromBody] AddUserRequest request)
        {
            var user = new User(request.Name, request.Email);
            _organizationService.AddUser(await _organizationQueries.GetDomainAsync(id), user, request.Role);
            return NoContent();
        }
    }
}
