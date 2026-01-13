using Microsoft.Extensions.Logging;
using Recuria.Application.Interface;
using Recuria.Domain;
using Recuria.Domain.Entities;
//using Recuria.Infrastructure.Observability;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application
{
    public class SubscriptionLifecycleOrchestrator : ISubscriptionLifecycleOrchestrator
    {
        private readonly IBillingService _billingService;
        private readonly IBillingRetryPolicy _retryPolicy;
        private readonly ILogger<SubscriptionLifecycleOrchestrator> _logger;

        public SubscriptionLifecycleOrchestrator(
            IBillingService billingService,
            IBillingRetryPolicy retryPolicy,
            ILogger<SubscriptionLifecycleOrchestrator> logger)
        {
            _billingService = billingService;
            _retryPolicy = retryPolicy;
            _logger = logger;
        }

        public void Process(Subscription subscription, DateTime now)
        {
            _logger.LogInformation(
            "Processing subscription {SubscriptionId} with status {Status}",
            subscription.Id,
            subscription.Status);

            switch (subscription.Status)
            { 
                case SubscriptionStatus.Trial:
                    HandleTrial(subscription, now);
                    break;

                case SubscriptionStatus.Active:
                    HandleActive(subscription, now);
                    break;

                case SubscriptionStatus.PastDue: 
                    HandlePastDue(subscription, now);
                    break;

                case SubscriptionStatus.Canceled:
                case SubscriptionStatus.Expired:
                    break;

                default:
                    throw new InvalidOperationException($"Unhandled subscription status: {subscription.Status}");
            }            
        }

        private void HandleTrial(Subscription subscription, DateTime now)
        {
            if( now >= subscription.PeriodEnd)
            {
                _logger.LogInformation("Trial expired for subscription {SubscriptionId}", subscription.Id);
                subscription.Expire(now);
            }
        }

        private void HandleActive(Subscription subscription, DateTime now)
        {
            if (now < subscription.PeriodEnd)
            {
                _logger.LogDebug(
                    "Subscription {SubscriptionId} still within billing period",
                    subscription.Id);
                return;
            }             

            var attempt = 0;

            while (true)
            {
                try
                {
                    attempt++;

                    _logger.LogInformation(
                        "Running billing cycle for subscription {SubscriptionId}, attempt {Attempt}",
                        subscription.Id,
                        attempt);

                    _billingService.RunBillingCycle(subscription, now);

                    subscription.RecordBillingAttempt(BillingAttempt.Success(subscription.Id));
                    subscription.AdvancePeriod(now);

                    _logger.LogInformation(
                        "Billing succeeded for subscription {SubscriptionId}",
                        subscription.Id);

                    return;
                }
                catch (Exception ex)
                {
                    subscription.RecordBillingAttempt(
                        BillingAttempt.Failure(subscription.Id, ex.Message));

                    _logger.LogWarning(ex,
                        "Billing attempt {Attempt} failed for subscription {SubscriptionId}",
                        attempt,
                        subscription.Id);

                    if (!_retryPolicy.ShouldRetry(attempt, ex))
                    {
                        subscription.MarkPastDue();

                        _logger.LogWarning(
                            "Subscription {SubscriptionId} marked PastDue after {Attempts} failed billing attempts",
                            subscription.Id,
                            attempt);

                        return;
                    }
                }
            }
        }

        private void HandlePastDue(Subscription subscription, DateTime now)
        {
            _logger.LogWarning(
               "Handling overdue subscription {SubscriptionId}",
               subscription.Id);
            _billingService.HandleOverdueSubscription(subscription, now);
        }
    }
}
