using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts.Invoice
{
    public sealed record InvoiceDetailsDto(
        Guid Id,
        string InvoiceNumber,
        DateTime IssuedOnUtc,
        DateTime? PaidOnUtc,
        MoneyDto Subtotal,
        MoneyDto Tax,
        MoneyDto Total,
        string Status
    );
}
