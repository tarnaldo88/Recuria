using Recuria.Application.Contracts.Subscription;
using Recuria.Application.Interface;
using Recuria.Domain;
using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Recuria.Application
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptions;
        private readonly IOrganizationRepository _organizations;
        private readonly ISubscriptionQueries _queries;

        public SubscriptionService(
        ISubscriptionRepository subscriptions,
        IOrganizationRepository organizations,
        ISubscriptionQueries queries)
        {
            _subscriptions = subscriptions;
            _organizations = organizations;
            _queries = queries;
        }

        public void ActivateSubscription(Subscription subscription)
        {
            subscription.Activate(DateTime.UtcNow);
        }

        public void UpgradePlan(Subscription subscription, PlanType newPlan)
        {
            subscription.UpgradePlan(newPlan);
        }

        public void CancelSubscription(Subscription subscription)
        {
            subscription.Cancel();
        }

        public Invoice GenerateInvoice(Subscription subscription, decimal amount)
        {
            if (subscription.Status != SubscriptionStatus.Active)
                throw new InvalidOperationException("Invoices can only be generated for active subscriptions.");

            return new Invoice(subscription.Id, amount);
        }

        public Subscription CreateTrial(Organization organization)
        {
            if(organization.GetCurrentSubscription(DateTime.UtcNow) != null)
            {
                throw new InvalidOperationException("Organization already has an active subscription.");
            }

            var now = DateTime.UtcNow;
            var subscription = Subscription.CreateTrial(organization, now); 

            organization.AssignSubscription(subscription);

            return subscription;
        }

        public Task<SubscriptionDetailsDto> CreateTrialAsync(Guid organizationId, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task UpgradeAsync(Guid subscriptionId, PlanType newPlan, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task CancelAsync(Guid subscriptionId, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
