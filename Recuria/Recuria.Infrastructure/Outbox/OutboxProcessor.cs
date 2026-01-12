using Microsoft.EntityFrameworkCore;
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

        public OutboxProcessor(
            RecuriaDbContext db,
            IDomainEventDispatcher dispatcher)
        {
            _db = db;
            _dispatcher = dispatcher;
        }
            
        public async Task ProcessAsync(CancellationToken ct)
        {
            var messages = await _db.OutBoxMessages
                .Where(m =>
                    m.ProcessedOnUtc == null &&
                    (m.NextAttemptOnUtc == null || m.NextAttemptOnUtc <= DateTime.UtcNow))
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

                    await _dispatcher.DispatchAsync(domainEvent, ct);

                    message.MarkProcessed();
                }
                catch (Exception ex)
                {
                    message.MarkFailed(ex.ToString());
                }
            }

            await _db.SaveChangesAsync(ct);
        }

    }
}
