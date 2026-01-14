using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Observability
{
    internal sealed class NullScope : IDisposable
    {
        public static readonly NullScope? Instance = new();
        public void Dispose() { }
    }
}
