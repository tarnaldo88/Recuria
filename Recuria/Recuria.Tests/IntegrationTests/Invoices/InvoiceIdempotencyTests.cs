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
    public async Task CreateInvoice_ConcurrentRequests_WithSameKeyAndPayload_Should_ReturnSingleResource()
    {
        // Skip this scenario on EF InMemory because unique indexes/concurrency are not enforced there.
        using (var providerScope = Factory.Services.CreateScope())
        {
            var providerDb = providerScope.ServiceProvider.GetRequiredService<RecuriaDbContext>();
            var provider = providerDb.Database.ProviderName ?? string.Empty;

            if (provider.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
                return;
        }

        var orgId = await SeedOrganizationWithActiveSubscriptionAsync();
        SetAuthHeader(Guid.NewGuid(), orgId, UserRole.Owner);

        var idemKey = $"invoice-create-{Guid.NewGuid():N}";
        var description = $"race-{Guid.NewGuid():N}";
        var amount = 49.00;

        var request1 = BuildCreateInvoiceRequest(orgId, amount, description, idemKey);
        var request2 = BuildCreateInvoiceRequest(orgId, amount, description, idemKey);

        var task1 = Client.SendAsync(request1);
        var task2 = Client.SendAsync(request2);

        await Task.WhenAll(task1, task2);

        var responses = new[] { task1.Result, task2.Result };

        responses.Count(r => r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.OK)
            .Should().Be(2);

        responses.Should().Contain(r => r.StatusCode == HttpStatusCode.Created);
        responses.Should().Contain(r => r.StatusCode == HttpStatusCode.OK);

        var ids = new List<Guid>();
        foreach (var response in responses)
        {
            var id = await response.Content.ReadFromJsonAsync<Guid>(JsonOptions);
            ids.Add(id);
        }

        ids.Distinct().Should().HaveCount(1);

        using var verifyScope = Factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<RecuriaDbContext>();

        var count = await verifyDb.Invoices
            .AsNoTracking()
            .Include(i => i.Subscription)
            .CountAsync(i =>
                i.Subscription.OrganizationId == orgId &&
                i.Description == description);

        count.Should().Be(1);
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

    [Fact]
    public async Task CreateInvoice_WithExpiredIdempotencyKey_Should_AllowReuse_AsNewRequest()
    {
        var orgId = await SeedOrganizationWithActiveSubscriptionAsync();
        SetAuthHeader(Guid.NewGuid(), orgId, UserRole.Owner);

        var idemKey = $"invoice-create-{Guid.NewGuid():N}";
        var firstDescription = $"ttl-first-{Guid.NewGuid():N}";
        var secondDescription = $"ttl-second-{Guid.NewGuid():N}";

        var firstRequest = BuildCreateInvoiceRequest(orgId, 49.00, firstDescription, idemKey);
        var firstResponse = await Client.SendAsync(firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstInvoiceId = await firstResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        // Force invoice age older than default TTL (24h) so key is treated as expired.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RecuriaDbContext>();
            var record = await db.ApiIdempotencyRecords
                .FirstAsync(x => x.OrganizationId == orgId && x.Operation == "invoice.create" && x.IdempotencyKey == idemKey);

            record.CreatedOnUtc = DateTime.UtcNow.AddDays(-2);
            await db.SaveChangesAsync();
        }

        var secondRequest = BuildCreateInvoiceRequest(orgId, 79.00, secondDescription, idemKey);
        var secondResponse = await Client.SendAsync(secondRequest);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var verifyScope = Factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<RecuriaDbContext>();

        var createdCount = await verifyDb.Invoices
            .AsNoTracking()
            .Include(i => i.Subscription)
            .CountAsync(i =>
                i.Subscription.OrganizationId == orgId &&
                (i.Description == firstDescription || i.Description == secondDescription));

        createdCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateInvoice_WithoutIdempotencyKey_Should_ReturnBadRequest()
    {
        var orgId = await SeedOrganizationWithActiveSubscriptionAsync();
        SetAuthHeader(Guid.NewGuid(), orgId, UserRole.Owner);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/invoices")
        {
            Content = JsonContent.Create(new
            {
                organizationId = orgId,
                amount = 49.00,
                description = "missing-key-test"
            })
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Idempotency-Key header is required.");
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
