using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Interface
{
    public interface ISubscriptionRepository
    {
        Task<Subscription> GetByOrganizationIdAsync(Guid organizationId);

        Task<Subscription> GetByIdAsync(Guid id);
        Task AddAsync(Subscription subscription);
        Task UpdateAsync(Subscription subscription);
    }
}
