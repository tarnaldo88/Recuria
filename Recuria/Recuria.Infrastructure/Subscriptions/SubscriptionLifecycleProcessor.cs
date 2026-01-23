using Microsoft.Extensions.Logging;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Subscriptions
{
    public sealed class SubscriptionLifecycleProcessor
    {
        private readonly ISubscriptionRepository _subscriptions;
        private readonly ISubscriptionLifecycleOrchestrator _orchestrator;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SubscriptionLifecycleProcessor> _logger;

        public SubscriptionLifecycleProcessor(
            ISubscriptionRepository subscriptions,
            ISubscriptionLifecycleOrchestrator orchestrator,
            IUnitOfWork uow,
            ILogger<SubscriptionLifecycleProcessor> logger)
        {
            _subscriptions = subscriptions;
            _orchestrator = orchestrator;
            _uow = uow;
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

                try
                {
                    _orchestrator.Process(subscription, now);
                    _subscriptions.Update(subscription);
                    await _uow.CommitAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Subscription lifecycle processing failed for {SubscriptionId}",
                        subscription.Id);
                }
            }
        }
    }
}
