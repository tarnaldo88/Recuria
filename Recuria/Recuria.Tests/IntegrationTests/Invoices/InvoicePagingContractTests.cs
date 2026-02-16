using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Contracts.Common;
using Recuria.Application.Contracts.Invoice;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Infrastructure.Persistence;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace Recuria.Tests.IntegrationTests.Invoices;

public sealed class InvoicePagingContractTests : IntegrationTestBase
{
    public InvoicePagingContractTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetInvoices_PagingAndTotalCount_Should_Respect_Bounds()
    {
        var (orgId, _) = await SeedInvoicesAsync(invoiceCount: 8);
        SetAuthHeader(Guid.NewGuid(), orgId, UserRole.Owner);

        var response = await Client.GetAsync($"/api/invoices/organization/{orgId}?page=0&pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<InvoiceListItemDto>>(JsonOptions);
        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(5);
        page.TotalCount.Should().Be(8);
        page.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetInvoices_SortAndSearch_Should_Return_Correct_Set()
    {
        var (orgId, _) = await SeedInvoicesAsync(invoiceCount: 8);
        SetAuthHeader(Guid.NewGuid(), orgId, UserRole.Owner);

        var searchResponse = await Client.GetAsync($"/api/invoices/organization/{orgId}?page=1&pageSize=10&search=matchme");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchPage = await searchResponse.Content.ReadFromJsonAsync<PagedResult<InvoiceListItemDto>>(JsonOptions);
        searchPage.Should().NotBeNull();
        searchPage!.TotalCount.Should().Be(2);
        searchPage.Items.Should().HaveCount(2);
        searchPage.Items.Should().OnlyContain(i => i.Status == "Unpaid");

        var sortResponse = await Client.GetAsync($"/api/invoices/organization/{orgId}?page=1&pageSize=10&sortBy=total&sortDir=desc");
        sortResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sortPage = await sortResponse.Content.ReadFromJsonAsync<PagedResult<InvoiceListItemDto>>(JsonOptions);
        sortPage.Should().NotBeNull();
        sortPage!.Items.Select(i => i.Total.Amount).Should().BeInDescendingOrder();
        sortPage.TotalCount.Should().Be(8);
    }

    private async Task<(Guid OrgId, Guid SubscriptionId)> SeedInvoicesAsync(int invoiceCount)
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

        for (var i = 1; i <= invoiceCount; i++)
        {
            var description = i is 3 or 5 ? $"matchme invoice {i}" : $"invoice {i}";
            var invoice = new Invoice(sub.Id, i * 10m, description) { Id = Guid.NewGuid() };
            db.Invoices.Add(invoice);
        }

        await uow.CommitAsync(CancellationToken.None);
        return (org.Id, sub.Id);
    }
}

