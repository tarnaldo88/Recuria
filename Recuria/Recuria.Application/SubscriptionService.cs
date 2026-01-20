using Recuria.Application.Contracts.Subscription;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Validation;
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
        private readonly ValidationBehavior _validator;
        private readonly IUnitOfWork _uow;

        public SubscriptionService( ISubscriptionRepository subscriptions, IOrganizationRepository organizations, ISubscriptionQueries queries, ValidationBehavior validator, IUnitOfWork uow)
        {
            _subscriptions = subscriptions;
            _organizations = organizations;
            _queries = queries;
            _validator = validator;
            _uow = uow;
        }

        public void ActivateSubscription(Subscription subscription)
        {
            subscription.Activate(DateTime.UtcNow);
        }

        public void UpgradePlan(Subscription subscription, PlanType newPlan)
        {
            subscription.UpgradePlan(newPlan);
        }

        public async void CancelSubscription(Subscription subscription, CancellationToken ct)
        {
            subscription.Cancel();
            _subscriptions.Update(subscription);
            
            await _uow.CommitAsync(ct);
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

        public async Task<SubscriptionDetailsDto> CreateTrialAsync(Guid organizationId, CancellationToken ct)
        {
            var org = await _organizations.GetByIdAsync(organizationId, ct);
            await _validator.ValidateAsync(org);

            if (org == null)
                throw new InvalidOperationException("Organization not found.");

            if (org.GetCurrentSubscription(DateTime.UtcNow) != null)
                throw new InvalidOperationException("Organization already has an active subscription.");

            var subscription = Subscription.CreateTrial(org, DateTime.UtcNow);

            org.AssignSubscription(subscription);

            await _subscriptions.AddAsync(subscription, ct);
            await _uow.CommitAsync(ct);

            return (await _queries.GetCurrentAsync(organizationId, ct))!;
        }

        public async Task UpgradeAsync(
            Guid subscriptionId,
            PlanType newPlan,
            CancellationToken ct)
        {
            var subscription = await _subscriptions.GetByIdAsync(subscriptionId, ct);
            await _validator.ValidateAsync(subscription);

            if (subscription == null)
                throw new InvalidOperationException("Not found");

            subscription.UpgradePlan(newPlan);

            _subscriptions.Update(subscription);
            await _uow.CommitAsync(ct);
        }

        public async Task CancelAsync(
            Guid subscriptionId,
            CancellationToken ct)
        {
            var subscription = await _subscriptions.GetByIdAsync(subscriptionId, ct);
            await _validator.ValidateAsync(subscription);

            if (subscription == null)
                throw new InvalidOperationException("Not found");

            subscription.Cancel();

            _subscriptions.Update(subscription);
            await _uow.CommitAsync(ct);
        }

        public async void ActivateAsync(Guid subscriptionId, CancellationToken ct = default)
        {
            var subscription = await _subscriptions.GetByIdAsync(subscriptionId, ct);
            await _validator.ValidateAsync(subscription);

            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            // Call domain method
            subscription.Activate(DateTime.UtcNow);

            // Persist changes via repository/unit of work
            _subscriptions.Update(subscription);
            await _uow.CommitAsync(ct);
        }
    }
}
