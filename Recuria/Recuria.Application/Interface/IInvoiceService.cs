using Recuria.Application.Contracts.Common;
using Recuria.Domain;
using Recuria.Domain.Entities;

namespace Recuria.Application.Interface
{
    public interface IInvoiceService
    {
        Task<Guid> CreateInvoiceAsync(
            Guid subscriptionId,
            MoneyDto amount,
            string? description,
            CancellationToken ct);

        Task MarkPaidAsync(Guid invoiceId, CancellationToken ct);

        Task<Invoice> GenerateFirstInvoice(Subscription subscription, CancellationToken ct = default);
    }
}
