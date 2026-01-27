using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Recuria.Infrastructure.Persistence.Locking;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Subscriptions
{
    public sealed class SubscriptionLifecycleHostedService : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
        private const string LockName = "SubscriptionLifecycleLock";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionLifecycleHostedService> _logger;

        public SubscriptionLifecycleHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionLifecycleHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var distributedLock = scope.ServiceProvider.GetRequiredService<IDatabaseDistributedLock>();
                    var processor = scope.ServiceProvider.GetRequiredService<SubscriptionLifecycleProcessor>();

                    if (await distributedLock.TryAcquireAsync(LockName, stoppingToken))
                    {
                        try
                        {
                            await processor.ProcessAsync(stoppingToken);
                        }
                        finally
                        {
                            await distributedLock.ReleaseAsync(LockName, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Subscription lifecycle processing failed");
                }

                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }
}
