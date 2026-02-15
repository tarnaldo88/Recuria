using Microsoft.EntityFrameworkCore;
using Recuria.Application.Contracts.Common;
using Recuria.Application.Contracts.Invoice;
using Recuria.Application.Interface;

namespace Recuria.Infrastructure.Persistence.Queries
{
    public sealed class InvoiceQueries : IInvoiceQueries
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
                .Where(i => i.Subscription.OrganizationId == organizationId)
                .OrderByDescending(i => i.InvoiceDate)
                .Select(i => new InvoiceListItemDto(
                    i.Id,
                    i.InvoiceDate,
                    new MoneyDto(i.Amount, "USD"),
                    i.Paid ? "Paid" : "Unpaid"
                ))
                .ToListAsync(ct);
        }

        public async Task<InvoiceDetailsDto?> GetDetailsAsync(
            Guid invoiceId,
            CancellationToken ct)
        {
            var result = await _db.Invoices
                .Where(i => i.Id == invoiceId)
                .Select(i => new
                {
                    i.Id,
                    i.InvoiceDate,
                    i.Paid,
                    i.PaidOnUtc,
                    i.Description,
                    i.Amount
                })
                .FirstOrDefaultAsync(ct);

            if (result is null)
                return null;

            return new InvoiceDetailsDto(
                result.Id,
                result.Id.ToString()[..8],
                result.InvoiceDate,
                result.PaidOnUtc,
                result.Description,
                new MoneyDto(result.Amount, "USD"),
                new MoneyDto(0, "USD"),
                new MoneyDto(result.Amount, "USD"),
                result.Paid ? "Paid" : "Unpaid"
            );
        }

        public async Task<PagedResult<InvoiceListItemDto>> GetForOrganizationPagedAsync(Guid orgId, TableQuery query, CancellationToken ct)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 5, 100);

            var q = _db.Invoices.AsNoTracking().Where(i => i.Subscription.OrganizationId == orgId);

            if(!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
            }
        }
    }
}
