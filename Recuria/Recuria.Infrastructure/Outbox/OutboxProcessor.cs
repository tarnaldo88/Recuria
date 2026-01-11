using Recuria.Domain.Abstractions;
using Recuria.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    }
}
