using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Recuria.Domain.Abstractions;
using Recuria.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Outbox
{
    public sealed class OutboxProcessor
    {
        private readonly RecuriaDbContext _db;
        private readonly IDomainEventDispatcher _dispatcher;
        private readonly IDatabaseDistributedLock _lock;
        private readonly ILogger<OutboxProcessor> _logger;
        private const string LockName = "OutboxProcessorLock";

        public OutboxProcessor(
            RecuriaDbContext db,
            IDomainEventDispatcher dispatcher,
            IDatabaseDistributedLock distributedLock,
            ILogger<OutboxProcessor> logger)
        {
            _db = db;
            _dispatcher = dispatcher;
            _lock = distributedLock;
            _logger = logger;
        }
            
        public async Task ProcessAsync(CancellationToken ct)
        {
            if (!await _lock.TryAcquireAsync(LockName, ct))
                return; // another instance is running

            try
            {
                var messages = await _db.OutBoxMessages
                    .Where(m => m.ProcessedOnUtc == null && (m.NextRetryOnUtc == null || m.NextRetryOnUtc <= DateTime.UtcNow))
                    .OrderBy(m => m.OccurredOnUtc)
                    .Take(20)
                    .ToListAsync(ct);

                foreach (var message in messages)
                {
                    try
                    {
                        var type = Type.GetType(message.Type)!;
                        var domainEvent = (IDomainEvent)
                            JsonSerializer.Deserialize(message.Content, type)!;

                        await _dispatcher.DispatchAsync(new[] { domainEvent }, ct);

                        message.ProcessedOnUtc = DateTime.UtcNow;

                        _logger.LogDebug(
                            "Dispatching outbox message {MessageId} of type {Type}",
                            message.Id,
                            message.Type);
                    }
                    catch (Exception ex)
                    {
                        message.Error = ex.Message;
                        message.RetryCount++;
                        message.NextRetryOnUtc =
                            DateTime.UtcNow.AddMinutes(Math.Pow(2, message.RetryCount));

                        _logger.LogError(ex,
                            "Failed to process outbox message {MessageId}",
                            message.Id);
                    }
                    _logger.LogInformation(
                        "Outbox message {MessageId} processed successfully",
                        message.Id);
                }

                await _db.SaveChangesAsync(ct);
            }
            finally
            {
                await _lock.ReleaseAsync(LockName, ct);
            }
        }

    }
}
