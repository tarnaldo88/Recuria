using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain.Events
{
    public interface IDomainEvent
    {
        DateTime OccuredOn { get; }
    }
}
