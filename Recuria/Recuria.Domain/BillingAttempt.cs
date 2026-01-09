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
    }
}
