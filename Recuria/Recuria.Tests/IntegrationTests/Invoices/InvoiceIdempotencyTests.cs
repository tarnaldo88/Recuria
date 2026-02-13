using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Infrastructure.Persistence;
using Recuria.Tests.IntegrationTests.Infrastructure;

namespace Recuria.Tests.IntegrationTests.Invoices;

public sealed class InvoiceIdempotencyTests : IntegrationTestBase
{
    public InvoiceIdempotencyTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateInvoice_WithSameIdempotencyKey_AndSamePayload_Should_ReturnSameInvoice()
    {
        var orgId = await SeedOrganizationWithActiveSubscriptionAsync();
        SetAuthHeader(Guid.NewGuid(), orgId, UserRole.Owner);

        var idemKey = $"invoice-create-{Guid.NewGuid():N}";
        var description = $"idem-{Guid.NewGuid():N}";
        var amount = 49.00;

        var firstRequest = BuildCreateInvoiceRequest(orgId, amount, description, idemKey);
        var firstResponse = await Client.SendAsync(firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstInvoiceId = await firstResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        var secondRequest = BuildCreateInvoiceRequest(orgId, amount, description, idemKey);
        var secondResponse = await Client.SendAsync(secondRequest);

        // Expected enterprise behavior: replay returns existing resource.
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondInvoiceId = await secondResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);
        secondInvoiceId.Should().Be(firstInvoiceId);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RecuriaDbContext>();
        var createdCount = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Subscription)
            .CountAsync(i => i.Subscription.OrganizationId == orgId && i.Description == description);
        createdCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateInvoice_WithSameIdempotencyKey_AndDifferentPayload_Should_ReturnConflict()
    {
        var orgId = await SeedOrganizationWithActiveSubscriptionAsync();
        SetAuthHeader(Guid.NewGuid(), orgId, UserRole.Owner);

        var idemKey = $"invoice-create-{Guid.NewGuid():N}";

        var firstRequest = BuildCreateInvoiceRequest(orgId, 49.00, "first-payload", idemKey);
        var firstResponse = await Client.SendAsync(firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondRequest = BuildCreateInvoiceRequest(orgId, 79.00, "different-payload", idemKey);
        var secondResponse = await Client.SendAsync(secondRequest);

        // Expected enterprise behavior: reject key reuse with different payload.
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private static HttpRequestMessage BuildCreateInvoiceRequest(Guid orgId, double amount, string description, string idempotencyKey)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/invoices")
        {
            Content = JsonContent.Create(new
            {
                organizationId = orgId,
                amount,
                description
            })
        };
        message.Headers.Add("Idempotency-Key", idempotencyKey);
        return message;
    }

    private async Task<Guid> SeedOrganizationWithActiveSubscriptionAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var orgs = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
        var subs = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var owner = new User($"{Guid.NewGuid():N}@test.local", "Owner");
        await users.AddAsync(owner, CancellationToken.None);

        var org = new Organization($"Org-{Guid.NewGuid():N}");
        org.AddUser(owner, UserRole.Owner);
        await orgs.AddAsync(org, CancellationToken.None);

        var now = DateTime.UtcNow;
        var sub = new Subscription(
            org,
            PlanType.Pro,
            SubscriptionStatus.Active,
            now.AddDays(-1),
            now.AddDays(29));
        await subs.AddAsync(sub, CancellationToken.None);

        await uow.CommitAsync(CancellationToken.None);
        return org.Id;
    }
}
