using Microsoft.EntityFrameworkCore;
using Recuria.Application.Interface.Idempotency;
using Recuria.Infrastructure.Persistence;
using Recuria.Infrastructure.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Idempotency
{
    public sealed class EfProcessedEventStore : IProcessedEventStore
    {
        private readonly RecuriaDbContext _db;

        public EfProcessedEventStore(RecuriaDbContext db)
        {
            _db = db;
        }

        public Task<bool> ExistsAsync(Guid eventId, string handler, CancellationToken ct)
            => _db.ProcessedEvents.AnyAsync(x => x.EventId == eventId && x.Handler == handler, ct);

        public async Task MarkProcessedAsync(Guid eventId, string handler, CancellationToken ct)
        {
            _db.ProcessedEvents.Add(new ProcessedEvent
            (
                eventId,
                handler,
                DateTime.UtcNow
            ));
            await _db.SaveChangesAsync(ct);
        }
    }
}
