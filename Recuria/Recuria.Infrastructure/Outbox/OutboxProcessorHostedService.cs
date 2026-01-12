using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Outbox
{
    public sealed class OutboxProcessorHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxProcessorHostedService> _logger;

        public OutboxProcessorHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxProcessorHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
    }
}
