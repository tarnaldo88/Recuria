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
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly RecuriaDbContext _context;

        public InvoiceRepository(RecuriaDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<Invoice>> GetBySubscriptionIdAsync(Guid subscriptionId)
        {
            return await _context.Invoices
                .Where(i => i.SubscriptionId == subscriptionId)
                .ToListAsync();
        }
    }
}
