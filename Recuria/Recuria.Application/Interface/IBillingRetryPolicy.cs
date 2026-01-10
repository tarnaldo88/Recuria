using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Interface
{
    public interface IBillingRetryPolicy
    {
        bool ShouldRetry(int attempt, Exception exception);
        TimeSpan GetRetryDelay(int attempt);
    }
}
