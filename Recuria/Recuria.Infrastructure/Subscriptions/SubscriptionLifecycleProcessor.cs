using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Infrastructure.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Subscriptions
{
    public sealed class SubscriptionLifecycleProcessor
    {
        private readonly ISubscriptionRepository _subscriptions;
        private readonly ISubscriptionLifecycleOrchestrator _orchestrator;
        private readonly IUnitOfWork _uow;
        private readonly RecuriaDbContext _db;
        private readonly ILogger<SubscriptionLifecycleProcessor> _logger;

        public SubscriptionLifecycleProcessor(
            ISubscriptionRepository subscriptions,
            ISubscriptionLifecycleOrchestrator orchestrator,
            IUnitOfWork uow,
            RecuriaDbContext db,
            ILogger<SubscriptionLifecycleProcessor> logger)
        {
            _subscriptions = subscriptions;
            _orchestrator = orchestrator;
            _uow = uow;
            _db = db;
            _logger = logger;
        }

        public async Task ProcessAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var due = await _subscriptions.GetDueForProcessingAsync(now, ct);

            if (due.Count == 0)
                return;

            foreach (var subscription in due)
            {
                ct.ThrowIfCancellationRequested();

                for (var attempt = 0; attempt < 2; attempt++)
                {
                    try
                    {
                        _orchestrator.Process(subscription, now);
                        _subscriptions.Update(subscription);
                        await _uow.CommitAsync(ct);
                        break;
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        if (attempt == 0)
                        {
                            await _db.Entry(subscription).ReloadAsync(ct);
                            continue;
                        }

                        _logger.LogWarning(ex,
                            "Subscription lifecycle concurrency conflict for {SubscriptionId}",
                            subscription.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Subscription lifecycle processing failed for {SubscriptionId}",
                            subscription.Id);
                        break;
                    }
                }
            }
        }
    }
}
