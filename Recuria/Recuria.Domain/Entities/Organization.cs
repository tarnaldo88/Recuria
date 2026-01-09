using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain.Entities
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

        public Subscription? GetCurrentSubscription(DateTime now)
        {
            var activeSubscriptions = Subscriptions
                .Where(s =>
                    s.Status == SubscriptionStatus.Active &&
                    s.PeriodStart <= now &&
                    s.PeriodEnd >= now)
                .ToList();

            if (activeSubscriptions.Count > 1)
                throw new InvalidOperationException(
                    "Organization has multiple active subscriptions. Data integrity violation.");

            return activeSubscriptions.SingleOrDefault();
        }
    }


}
