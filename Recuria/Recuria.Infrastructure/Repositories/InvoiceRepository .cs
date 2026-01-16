using Microsoft.EntityFrameworkCore;
using Recuria.Application.Interface.Abstractions;
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
        private readonly RecuriaDbContext _db;

        public InvoiceRepository(RecuriaDbContext db)
        {
            _db = db;
        }

        public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct) =>
            _db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);

        public async Task AddAsync(Invoice invoice, CancellationToken ct)
        {
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) =>
            _db.SaveChangesAsync(ct);

        public async Task<IReadOnlyList<Invoice>> GetBySubscriptionIdAsync(Guid subscriptionId)
        {
            return await _db.Invoices
                .Where(i => i.SubscriptionId == subscriptionId)
                .ToListAsync();
        }
    }
}
