using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recuria.Api.Auth;
using Recuria.Application.Interface;
using Recuria.Application.Requests;
using Recuria.Domain;
using Recuria.Domain.Enums;
using Recuria.Infrastructure.Persistence;

namespace Recuria.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.OrganizationsManageUsers)]
[Route("api/onboarding")]
public sealed class OnboardingController : ControllerBase
{
    private readonly RecuriaDbContext _db;
    private readonly IOrganizationService _organizationService;

    public OnboardingController(RecuriaDbContext db, IOrganizationService organizationService)
    {
        _db = db;
        _organizationService = organizationService;
    }

    public sealed class ChecklistDto
    {
        public bool OrganizationCreated { get; init; }
        public bool HasSampleData { get; init; }
        public bool TeamInvited { get; init; }
        public int UserCount { get; init; }
        public int InvoiceCount { get; init; }
    }

    public sealed class SeedSampleDataRequest
    {
        public Guid OrganizationId { get; init; }
    }

    public sealed class SeedSampleDataResponse
    {
        public int UsersCreated { get; init; }
        public int InvoicesCreated { get; init; }
    }

    public sealed class InviteTeamRequest
    {
        public Guid OrganizationId { get; init; }
        public List<string> Emails { get; init; } = new();
    }

    [HttpGet("checklist/{organizationId:guid}")]
    public async Task<ActionResult<ChecklistDto>> GetChecklist(Guid organizationId, CancellationToken ct)
    {
        if (!User.IsInOrganization(organizationId))
            return Forbid();

        var orgExists = await _db.Organizations.AsNoTracking().AnyAsync(x => x.Id == organizationId, ct);
        var userCount = await _db.Users.AsNoTracking().CountAsync(x => x.OrganizationId == organizationId, ct);

        var subscriptionIds = await _db.Subscriptions.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var invoiceCount = await _db.Invoices.AsNoTracking()
            .CountAsync(x => subscriptionIds.Contains(x.SubscriptionId), ct);

        return Ok(new ChecklistDto
        {
            OrganizationCreated = orgExists,
            HasSampleData = invoiceCount > 0,
            TeamInvited = userCount > 1,
            UserCount = userCount,
            InvoiceCount = invoiceCount
        });
    }

    [HttpPost("seed-sample-data")]
    public async Task<ActionResult<SeedSampleDataResponse>> SeedSampleData([FromBody] SeedSampleDataRequest request, CancellationToken ct)
    {
        if (!User.IsInOrganization(request.OrganizationId))
            return Forbid();

        var org = await _db.Organizations
            .Include(x => x.Subscriptions)
            .Include(x => x.Users)
            .FirstOrDefaultAsync(x => x.Id == request.OrganizationId, ct);
        if (org is null)
            return NotFound();

        var createdUsers = 0;
        var createdInvoices = 0;

        if (!org.Users.Any(x => x.Email == "ops@demo.local"))
        {
            await _organizationService.AddUserAsync(request.OrganizationId, new AddUserRequest
            {
                UserId = Guid.NewGuid(),
                Email = "ops@demo.local",
                Name = "Operations Demo",
                Role = UserRole.Member
            }, ct);
            createdUsers++;
        }

        var subscription = org.Subscriptions
            .OrderByDescending(x => x.PeriodEnd)
            .FirstOrDefault(x => x.Status != SubscriptionStatus.Canceled && x.Status != SubscriptionStatus.Expired);
        if (subscription is null)
            return BadRequest("No active/trial subscription found to attach sample invoices.");

        var hasDemoInvoices = await _db.Invoices.AnyAsync(x =>
            x.SubscriptionId == subscription.Id &&
            x.Description != null &&
            x.Description.Contains("[sample]", StringComparison.OrdinalIgnoreCase), ct);

        if (!hasDemoInvoices)
        {
            var invoice1 = new Invoice(subscription.Id, 29m, "[sample] Starter recurring invoice");
            var invoice2 = new Invoice(subscription.Id, 49m, "[sample] One-time setup invoice");
            invoice1.MarkAsPaid();
            _db.Invoices.Add(invoice1);
            _db.Invoices.Add(invoice2);
            createdInvoices += 2;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new SeedSampleDataResponse
        {
            UsersCreated = createdUsers,
            InvoicesCreated = createdInvoices
        });
    }

    [HttpPost("invite-team")]
    public async Task<ActionResult<object>> InviteTeam([FromBody] InviteTeamRequest request, CancellationToken ct)
    {
        if (!User.IsInOrganization(request.OrganizationId))
            return Forbid();

        var normalizedEmails = request.Emails
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        var invited = 0;
        foreach (var email in normalizedEmails)
        {
            if (!System.Net.Mail.MailAddress.TryCreate(email, out _))
                continue;

            await _organizationService.AddUserAsync(request.OrganizationId, new AddUserRequest
            {
                UserId = Guid.NewGuid(),
                Email = email,
                Name = email.Split('@')[0],
                Role = UserRole.Member
            }, ct);
            invited++;
        }

        return Ok(new { invited });
    }
}
