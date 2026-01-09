using Recuria.Application;
using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Tests.Fakes
{
    public class FakeBillingService : IBillingService
    {
        private readonly Queue<Exception?> _results = new();

        public void QueueFailure(Exception exception) => _results.Enqueue(exception);
        public void QueueSuccess() => _results.Enqueue(null);

        public Invoice RunBillingCycle(Subscription subscription, DateTime now)
        {
            if (_results.Count == 0)
                throw new InvalidOperationException("No billing result queued.");

            var result = _results.Dequeue();

            if (result != null)
                throw result;

            return new Invoice(subscription.Id, 29m);
        }
    }
}
