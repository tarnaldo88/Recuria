using Recuria.Application.Contracts.Common;
using Recuria.Application.Contracts.Invoice;
using Recuria.Infrastructure.Persistence.Queries.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence.Queries
{
    internal sealed class InvoiceQueries : IInvoiceQueries
    {
        private readonly RecuriaDbContext _db;

        public InvoiceQueries(RecuriaDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<InvoiceListItemDto>> GetForOrganizationAsync(
            Guid organizationId,
            CancellationToken ct)
        {
            return await _db.Invoices
                .Where(i => i.OrganizationId == organizationId)
                .OrderByDescending(i => i.IssuedOnUtc)
                .Select(i => new InvoiceListItemDto(
                    i.Id,
                    i.IssuedOnUtc,
                    new MoneyDto(
                        i.Total.Amount,
                        i.Total.Currency
                    ),
                    i.Status.ToString()
                ))
                .ToListAsync(ct);
        }

        public async Task<InvoiceDetailsDto?> GetDetailsAsync(
            Guid invoiceId,
            CancellationToken ct)
        {
            return await _db.Invoices
                .Where(i => i.Id == invoiceId)
                .Select(i => new InvoiceDetailsDto(
                    i.Id,
                    i.InvoiceNumber,
                    i.IssuedOnUtc,
                    i.PaidOnUtc,
                    new MoneyDto(i.Subtotal.Amount, i.Subtotal.Currency),
                    new MoneyDto(i.Tax.Amount, i.Tax.Currency),
                    new MoneyDto(i.Total.Amount, i.Total.Currency),
                    i.Status.ToString()
                ))
                .FirstOrDefaultAsync(ct);
        }
    }
}
