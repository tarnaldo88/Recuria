using Recuria.Application.Contracts.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence.Queries
{
    public interface ISubscriptionQueries
    {        
        Task<SubscriptionDetailsDto?> GetCurrentAsync(
            Guid organizationId,
            CancellationToken ct);
    }
}
