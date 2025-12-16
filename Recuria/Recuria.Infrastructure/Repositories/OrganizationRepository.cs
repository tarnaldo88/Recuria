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
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly RecuriaDbContext _context;

        public OrganizationRepository(RecuriaDbContext context)
        {
            _context = context;
        }

        public async Task<Organization?> GetByIdAsync(Guid id)
        {
            return await _context.Organizations
                .Include(o => o.Users)
                .Include(o => o.GetCurrentSubscription())
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
    }
}
