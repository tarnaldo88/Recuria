using Recuria.Application.Contracts.Common;
using Recuria.Domain;
using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Interface
{
    public interface IInvoiceService
    {
        Task<Guid> CreateInvoiceAsync(
        Guid subscriptionId,
        MoneyDto amount,
        CancellationToken ct);

        Task MarkPaidAsync(Guid invoiceId, CancellationToken ct);

        Task<Invoice> GenerateFirstInvoice(Subscription subscription, CancellationToken ct = default);
    }
}
