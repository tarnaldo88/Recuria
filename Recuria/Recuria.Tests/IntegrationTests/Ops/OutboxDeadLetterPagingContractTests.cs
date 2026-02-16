using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Contracts.Common;
using Recuria.Domain.Enums;
using Recuria.Infrastructure.Outbox;
using Recuria.Infrastructure.Persistence;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace Recuria.Tests.IntegrationTests.Ops;

public sealed class OutboxDeadLetterPagingContractTests : IntegrationTestBase
{
    public OutboxDeadLetterPagingContractTests(CustomWebApplicationFactory factory) : base(factory) { }

    private sealed record DeadLetteredOutboxItemDto(
        Guid Id,
        DateTime OccurredOnUtc,
        DateTime DeadLetteredOnUtc,
        string Type,
        string? Error,
        int RetryCount);

    [Fact]
    public async Task GetDeadLettered_PagingAndTotalCount_Should_Respect_Bounds()
    {
        var marker = $"bounds-{Guid.NewGuid():N}";
        await SeedDeadLetteredOutboxAsync(itemCount: 8, marker: marker);
        SetAuthHeader(Guid.NewGuid(), Guid.NewGuid(), UserRole.Owner);

        var response = await Client.GetAsync($"/api/outbox/dead-lettered?page=0&pageSize=1&search={marker}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<DeadLetteredOutboxItemDto>>(JsonOptions);
        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(5);
        page.TotalCount.Should().Be(8);
        page.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetDeadLettered_SortAndSearch_Should_Return_Correct_Set()
    {
        var marker = $"matchme-{Guid.NewGuid():N}";
        const string needle = "needle-";
        await SeedDeadLetteredOutboxAsync(itemCount: 8, marker: marker);
        SetAuthHeader(Guid.NewGuid(), Guid.NewGuid(), UserRole.Owner);

        var searchResponse = await Client.GetAsync($"/api/outbox/dead-lettered?page=1&pageSize=10&search={needle}");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchPage = await searchResponse.Content.ReadFromJsonAsync<PagedResult<DeadLetteredOutboxItemDto>>(JsonOptions);
        searchPage.Should().NotBeNull();
        searchPage!.TotalCount.Should().Be(2);
        searchPage.Items.Should().HaveCount(2);
        searchPage.Items.Should().OnlyContain(i => i.Type.Contains(needle) || (i.Error ?? string.Empty).Contains(needle));

        var sortResponse = await Client.GetAsync($"/api/outbox/dead-lettered?page=1&pageSize=10&sortBy=retryCount&sortDir=desc&search={marker}");
        sortResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sortPage = await sortResponse.Content.ReadFromJsonAsync<PagedResult<DeadLetteredOutboxItemDto>>(JsonOptions);
        sortPage.Should().NotBeNull();
        sortPage!.Items.Select(i => i.RetryCount).Should().BeInDescendingOrder();
        sortPage.TotalCount.Should().Be(8);
    }

    private async Task SeedDeadLetteredOutboxAsync(int itemCount, string marker)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RecuriaDbContext>();

        for (var i = 1; i <= itemCount; i++)
        {
            var message = new OutBoxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOnUtc = DateTime.UtcNow.AddMinutes(-i),
                Type = i is 3 or 6 ? $"Billing.{marker}.needle-{i}" : $"Billing.{marker}.{i}",
                Content = "{}",
                RetryCount = i
            };

            message.MarkDeadLettered(i is 3 or 6 ? $"{marker}-needle-error-{i}" : $"{marker}-error-{i}");
            db.OutBoxMessages.Add(message);
        }

        await db.SaveChangesAsync(CancellationToken.None);
    }
}
