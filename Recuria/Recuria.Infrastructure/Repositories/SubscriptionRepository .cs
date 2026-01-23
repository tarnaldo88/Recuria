using Microsoft.EntityFrameworkCore;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
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

        public async Task UpdateAsync(Subscription subscription)
        {
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task<Subscription> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Subscriptions.FindAsync(id);
        }

        public async Task<List<Subscription>> GetDueForProcessingAsync(DateTime now, CancellationToken ct)
        {
            return await _context.Subscriptions
                .Where(s =>
                    (s.Status == SubscriptionStatus.Trial
                    || s.Status == SubscriptionStatus.Active
                    || s.Status == SubscriptionStatus.PastDue)
                    && s.PeriodEnd <= now)
                .ToListAsync(ct);
        }

        public async Task AddAsync(Subscription subscription, CancellationToken ct)
        {
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
        }

        public void Update(Subscription subscription)
        {
            if(subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            var entry = _context.Entry(subscription);
            
            if(entry.State == EntityState.Detached)
            {
                _context.Subscriptions.Attach(subscription);
            }

            entry.State = EntityState.Modified;
        }

        public void Remove(Subscription subscription)
        {
            _context.Subscriptions.Remove(subscription);
        }
    }
}
