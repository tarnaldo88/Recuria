using Recuria.Application.Interface.Idempotency;
using Recuria.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Idempotency
{
    internal sealed class EfProcessedEventStore : IProcessedEventStore
    {
        private readonly RecuriaDbContext _db;

        public EfProcessedEventStore(RecuriaDbContext db)
        {
            _db = db;
        }

        public Task<bool> ExistsAsync(Guid eventId, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task MarkProcessedAsync(Guid eventId, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
