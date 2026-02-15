using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts.Common
{
    public sealed class TableQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? Search { get; init; }
        public string? SortBy { get; init; }
        public string SortDir { get; init; } = "asc";
    }
}
