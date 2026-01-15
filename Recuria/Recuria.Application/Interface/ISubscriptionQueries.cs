using Recuria.Application.Contracts.Subscription;
using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Interface
{
    public interface ISubscriptionQueries
    {
        Task<SubscriptionDetailsDto?> GetCurrentAsync(Guid organizationId, CancellationToken ct);
        Task<Organization> GetDomainAsync(Guid organizationId);
        Task<Subscription> GetDomainByIdAsync(Guid subscriptionId);
    }
}
