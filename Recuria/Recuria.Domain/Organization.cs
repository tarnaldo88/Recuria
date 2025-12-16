using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain
{
    public class Organization
    {
        public Guid Id { get; private set; }   // make setter private
        public string Name { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public List<User> Users { get; private set; } = new();
        public ICollection<Subscription> Subscriptions { get; private set; }
            = new List<Subscription>();

        protected Organization() { } // EF Core

        public Organization(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

        public void AssignSubscription(Subscription subscription)
        {
            if (subscription.Organization != this)
                throw new InvalidOperationException(
                    "Subscription belongs to a different organization.");

            Subscriptions.Add(subscription);
        }

        public Subscription? GetCurrentSubscription()
        {
            return Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.PeriodStart)
                .FirstOrDefault();
        }
    }


}
