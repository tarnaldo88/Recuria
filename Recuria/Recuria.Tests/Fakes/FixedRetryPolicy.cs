using Recuria.Application;
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
    }
}
