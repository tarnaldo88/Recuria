using Recuria.Application.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Tests.Fakes
{
    public class FixedRetryPolicy : IBillingRetryPolicy
    {
        private readonly int _maxRetries;

        public FixedRetryPolicy(int maxRetries)
        {
            _maxRetries = maxRetries;
        }

        public TimeSpan GetRetryDelay(int attempt)
        {
            // throw new NotImplementedException();
            return TimeSpan.FromSeconds(1);
        }

        public bool ShouldRetry(int attempt, Exception ex)
        {
            return attempt < _maxRetries;
        }
    }
}
