using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Observability
{
    public static class RecuriaMetrics
    {
        public static readonly Meter Meter =
            new("Recuria.Subscriptions", "1.0");

        public static readonly Counter<long> BillingSuccess =
            Meter.CreateCounter<long>("billing.success");

        public static readonly Counter<long> BillingFailure =
            Meter.CreateCounter<long>("billing.failure");

        public static readonly Histogram<double> BillingDuration =
            Meter.CreateHistogram<double>("billing.duration.ms");
    }
}
