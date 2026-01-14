using Recuria.Application.Observability;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Observability
{
    public sealed class SubscriptionTelemetry : ISubscriptionTelemetry
    {
        public IDisposable BeginProcessing(Guid subscriptionId, string status)
        {
            var activity = Telemetry.ActivitySource.StartActivity(
                "SubscriptionLifecycle.Process");

            if (activity != null)
            {
                activity.SetTag("subscription.id", subscriptionId);
                activity.SetTag("subscription.status", status);
            }

            return new ActivityScope(activity);
        }

        public void BillingAttempted()
            => Telemetry.BillingAttempts.Add(1);

        public void BillingFailed()
            => Telemetry.BillingFailures.Add(1);

        public void SubscriptionActivated()
            => Telemetry.SubscriptionsActivated.Add(1);

        private class ActivityScope : IDisposable
        {
            private readonly Activity? _activity;

            public ActivityScope(Activity? activity) => _activity = activity;

            public void Dispose()
            {
                _activity?.Stop();
            }
        }
    }
}
