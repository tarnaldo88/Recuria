using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Observability
{
    public interface ISubscriptionTelemetry
    {
        IDisposable BeginProcessing(Guid subscriptionId, string status);

        void BillingAttempted();

        void BillingFailed();

        void SubscriptionActivated();
    }
}
