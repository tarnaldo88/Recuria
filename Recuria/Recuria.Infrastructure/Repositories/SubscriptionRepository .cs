using Microsoft.EntityFrameworkCore;
using Recuria.Application;
using Recuria.Domain;
using Recuria.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly RecuriaDbContext _context;

        public SubscriptionRepository(RecuriaDbContext context)
        {
            _context = context;
        }

        public async Task<Subscription?> GetByOrganizationIdAsync(Guid organizationId)
        {
            return await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);
        }

        public async Task<Subscription?> GetByIdAsync(Guid id)
        {
            return await _context.Subscriptions.FindAsync(id);
        }

        public async Task AddAsync(Subscription subscription)
        {
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Subscription subscription)
        {
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }
    }
}
