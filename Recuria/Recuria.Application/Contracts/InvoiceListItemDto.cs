using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts
{
    public sealed record InvoiceListItemDto(
        Guid Id,
        DateTime IssuedOnUtc,
        MoneyDto Total,
        string Status
    );
}
