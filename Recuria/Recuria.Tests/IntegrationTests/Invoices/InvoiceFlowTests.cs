using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Infrastructure.Persistence;
using Recuria.Tests.IntegrationTests.Infrastructure;

namespace Recuria.Tests.IntegrationTests.Invoices;

public sealed class InvoiceFlowTests : IntegrationTestBase
{
    public InvoiceFlowTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetInvoiceDetails_Should_Return_Description()
    {
        var (orgId, invoiceId) = await SeedInvoiceAsync("Seed description");
        SetAuthHeader(Guid.NewGuid(), orgId, UserRole.Owner);

        var response = await Client.GetAsync($"/api/invoices/{invoiceId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<Recuria.Application.Contracts.Invoice.InvoiceDetailsDto>(JsonOptions);
        dto.Should().NotBeNull();
        dto!.Description.Should().Be("Seed description");
    }

    [Fact]
    public async Task MarkPaid_Should_Set_PaidOnUtc()
    {
        var (orgId, invoiceId) = await SeedInvoiceAsync("Pay me");
        SetAuthHeader(Guid.NewGuid(), orgId, UserRole.Owner);

        var pay = await Client.PostAsync($"/api/invoices/{invoiceId}/pay", JsonContent.Create(new { }));
        pay.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var details = await Client.GetAsync($"/api/invoices/{invoiceId}");
        details.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await details.Content.ReadFromJsonAsync<Recuria.Application.Contracts.Invoice.InvoiceDetailsDto>(JsonOptions);
        dto!.PaidOnUtc.Should().NotBeNull();
    }

    private async Task<(Guid OrgId, Guid InvoiceId)> SeedInvoiceAsync(string description)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RecuriaDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var orgs = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
        var subs = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var owner = new User($"{Guid.NewGuid()}@test.local", "Owner");
        await users.AddAsync(owner, CancellationToken.None);

        var org = new Organization($"Org-{Guid.NewGuid()}");
        org.AddUser(owner, UserRole.Owner);
        await orgs.AddAsync(org, CancellationToken.None);

        var now = DateTime.UtcNow;
        var sub = new Subscription(org, PlanType.Pro, SubscriptionStatus.Active, now.AddDays(-1), now.AddDays(29));
        await subs.AddAsync(sub, CancellationToken.None);

        var invoice = new Invoice(sub.Id, 10m, description) { Id = Guid.NewGuid() };
        db.Invoices.Add(invoice);

        await uow.CommitAsync(CancellationToken.None);
        return (org.Id, invoice.Id);
    }
}
