using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
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
        private readonly ISubscriptionService _subscriptions;
        private readonly IOrganizationRepository _orgs;
        private readonly IUnitOfWork _uow;

        public CreateTrialOnOrganizationCreated(
            ISubscriptionService subscriptions,
            IOrganizationRepository orgs,
            IUnitOfWork uow)
        {
            _subscriptions = subscriptions;
            _orgs = orgs;
            _uow = uow;
        }

        public async Task HandleAsync(
            OrganizationCreatedDomainEvent @event,
            CancellationToken ct)
        {
            var org = await _orgs.GetByIdAsync(@event.OrganizationId, ct);

            if (org == null) { return; }

            await _subscriptions.CreateTrialAsync(org.Id, ct);

            await _uow.CommitAsync(ct);
        }
    }

}
