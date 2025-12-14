using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain
{
    public enum SubscriptionStatus
    {
        Trialing,
        Active,
        PastDue,
        Canceled
    }


    public class Subscription
    {
        public Guid Id { get; init; }
        public Guid OrganizationId { get; private set; }
    }
}
