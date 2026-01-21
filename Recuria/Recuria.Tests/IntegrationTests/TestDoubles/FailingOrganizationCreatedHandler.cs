using Recuria.Application.Interface;
using Recuria.Domain.Events.Organization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Tests.IntegrationTests.TestDoubles
{
    public sealed class FailingOrganizationCreatedHandler
    : IDomainEventHandler<OrganizationCreatedDomainEvent>
    {
        public Task HandleAsync(
            OrganizationCreatedDomainEvent domainEvent,
            CancellationToken ct)
        {
            throw new InvalidOperationException("Simulated failure");
        }
    }
}
