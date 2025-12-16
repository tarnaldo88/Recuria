using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain
{
    //internal class Organization
    //{
    //}

    public class Organization
    {
        public Guid Id { get; set; }
        public string Name { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public List<User> Users { get; private set; } = new();
        public ICollection<Subscription> Subscriptions { get; private set; } = new List<Subscription>();

        //public Subscription? CurrentSubscription { get; private set; }

        public Organization(string name) {
            Name = name;
        }

        public void AssignSubscription(Subscription subscription)
        {
            if (subscription.Organization != this)
                throw new InvalidOperationException("Subscription belongs to a different organization.");

            Subscriptions.Add(subscription);
        }

    }

}
