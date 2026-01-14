using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts.Common
{
    public sealed record MoneyDto(
        decimal Amount,
        string Currency
    );
}
