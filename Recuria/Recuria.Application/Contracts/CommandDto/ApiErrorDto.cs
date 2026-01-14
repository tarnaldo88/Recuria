using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts.CommandDto
{
    public sealed record ApiErrorDto(string Code, string Message);
}
