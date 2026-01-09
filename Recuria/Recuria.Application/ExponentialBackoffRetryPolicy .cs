using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application
{
    public class ExponentialBackoffRetryPolicy : IBillingRetryPolicy
    {
        private const int MaxAttempts = 3;

        public bool ShouldRetry(int attempt, Exception exception)
        {
            return attempt < MaxAttempts &&
                   exception is InvalidOperationException;
        }

        public TimeSpan GetRetryDelay(int attempt)
        {
            return TimeSpan.FromSeconds(Math.Pow(2, attempt));
        }
    }
}
