using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Requests;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Tests.IntegrationTests.Infrastructure;

namespace Recuria.Tests.IntegrationTests.Invoices;

public sealed class InvoiceContractTests : IntegrationTestBase
{
    public InvoiceContractTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateInvoice_Should_Return_Created_And_InvoiceId_And_Persist_Description()
    {
        var (orgId, _) = await SeedOrganizationWithActiveSubscriptionAsync();
        var userId = Guid.NewGuid();
        SetAuthHeader(userId, orgId, UserRole.Owner);

        var request = new CreateInvoiceRequest
        {
            OrganizationId = orgId,
            Amount = 42.50,
            Description = "Enterprise test invoice"
        };

        var createResponse = await Client.PostAsJsonAsync("/api/invoices", request, JsonOptions);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdId = await createResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);
        createdId.Should().NotBe(Guid.Empty);

        var detailsResponse = await Client.GetAsync($"/api/invoices/{createdId}");
        detailsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var details = await detailsResponse.Content
            .ReadFromJsonAsync<Recuria.Application.Contracts.Invoice.InvoiceDetailsDto>(JsonOptions);

        details.Should().NotBeNull();
        details!.Description.Should().Be("Enterprise test invoice");
    }

    private async Task<(Guid OrganizationId, Guid SubscriptionId)> SeedOrganizationWithActiveSubscriptionAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var orgs = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
        var subs = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var owner = new User($"{Guid.NewGuid()}@invoice.test", "Invoice Owner");
        await users.AddAsync(owner, CancellationToken.None);

        var org = new Organization($"Org-{Guid.NewGuid()}");
        org.AddUser(owner, UserRole.Owner);
        await orgs.AddAsync(org, CancellationToken.None);

        var now = DateTime.UtcNow;
        var subscription = new Subscription(
            org,
            PlanType.Pro,
            SubscriptionStatus.Active,
            now.AddDays(-1),
            now.AddDays(29));

        await subs.AddAsync(subscription, CancellationToken.None);
        await uow.CommitAsync(CancellationToken.None);

        return (org.Id, subscription.Id);
    }
}
