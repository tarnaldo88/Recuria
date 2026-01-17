using Microsoft.EntityFrameworkCore;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain.Entities;
using Recuria.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Repositories
{
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly RecuriaDbContext _context;

        public OrganizationRepository(RecuriaDbContext context)
        {
            _context = context;
        }

        public async Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Organizations
                .Include(o => o.Users)
                .Include(o => o.GetCurrentSubscription(DateTime.UtcNow))
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task AddAsync(Organization organization)
        {
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Organization organization)
        {
            _context.Organizations.Update(organization);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(
        Organization organization,
        CancellationToken ct)
        {
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) =>
            _context.SaveChangesAsync(ct);

        public void Update(Organization organization)
        {
            _context.Organizations.Update(organization);
        }
    }
}
