using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Observability
{
    public static class Telemetry
    {
        public const string ServiceName = "Recuria";

        public static readonly ActivitySource ActivitySource = new(ServiceName);

        public static readonly Meter Meter = new(ServiceName);

        // Metrics
        public static readonly Counter<long> BillingAttempts = Meter.CreateCounter<long>("billing_attempts_total");

        public static readonly Counter<long> BillingFailures = Meter.CreateCounter<long>("billing_failures_total");

        public static readonly Counter<long> SubscriptionsActivated = Meter.CreateCounter<long>("subscriptions_activated_total");
    }
}
