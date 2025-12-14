using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain
{
    public class Invoice
    {
        public Guid Id { get; init; }
        public Guid SubscriptionId { get; private set; }

    }
}
