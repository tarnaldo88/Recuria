using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain
{
    public class BillingAttempt
    {
        public Guid Id { get; }
        public Guid SubscriptionId { get; }
        public DateTime AttemptedAt { get; }
        public bool Succeeded { get; }
        public string? FailureReason { get; }

        private BillingAttempt(Guid subscriptionId, bool succeeded, string? failureReason)
        {
            Id = Guid.NewGuid();
            SubscriptionId = subscriptionId;
            AttemptedAt = DateTime.UtcNow;
            Succeeded = succeeded;
            FailureReason = failureReason;
        }    
    }
}
