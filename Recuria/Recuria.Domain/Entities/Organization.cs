using Recuria.Domain.Abstractions;
using Recuria.Domain.Events.Organization;
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
        public ICollection<Subscription> Subscriptions { get; private set; } = new List<Subscription>();

        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

        protected Organization() { } // EF Core

        public Organization(string name)
        {
            Id = Guid.NewGuid();
            Name = name;

            AddDomainEvent(new OrganizationCreatedDomainEvent(Id));
        }
        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
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

        public void AddUser(User user, UserRole role)
        {
            if (Users.Any(u => u.Id == user.Id))
                throw new InvalidOperationException("User already exists in organization.");

            user.AssignToOrganization(this, role);
            Users.Add(user);
        }

        public void ChangeUserRole(Guid userId, UserRole newRole)
        {
            var user = Users.FirstOrDefault(u => u.Id == userId)
                ?? throw new InvalidOperationException("User not found.");

            if (newRole == UserRole.Owner)
                throw new InvalidOperationException("Cannot assign owner role.");

            if (user.Role == UserRole.Owner)
                throw new InvalidOperationException("Cannot change owner role.");

            user.ChangeRole(newRole);
        }

        public void RemoveUser(Guid userId)
        {
            var user = Users.FirstOrDefault(u => u.Id == userId)
                ?? throw new InvalidOperationException("User not found.");

            if (user.Role == UserRole.Owner)
                throw new InvalidOperationException("Cannot remove owner.");

            Users.Remove(user);
        }
    }


}
