using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain.Entities;
using Recuria.Domain.Events.Organization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Organizations
{
    public sealed class CreateTrialOnOrganizationCreated: IDomainEventHandler<OrganizationCreatedDomainEvent>
    {
        private readonly ISubscriptionRepository _subscriptions;
        private readonly IOrganizationRepository _orgs;
        public CreateTrialOnOrganizationCreated(
            ISubscriptionRepository subscriptions,
            IOrganizationRepository orgs)
        {
            _subscriptions = subscriptions;
            _orgs = orgs;
        }

        public async Task HandleAsync(
            OrganizationCreatedDomainEvent @event,
            CancellationToken ct)
        {
            var org = await _orgs.GetByIdAsync(@event.OrganizationId, ct) ?? throw new InvalidOperationException("Organization not found."); 

            if (org.GetCurrentSubscription(DateTime.UtcNow) != null)
                return;

            var subscription = Subscription.CreateTrial(org, DateTime.UtcNow);

            //await _subscriptions.CreateTrialAsync(org.Id, ct);
            await _subscriptions.AddAsync(subscription, ct);
        }
    }

}
