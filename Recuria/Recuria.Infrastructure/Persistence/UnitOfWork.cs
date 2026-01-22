using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using Recuria.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly RecuriaDbContext _db;
        private readonly IDomainEventDispatcher _dispatcher;

        public UnitOfWork(RecuriaDbContext db, IDomainEventDispatcher dispatcher)
        {
            _db = db;
            _dispatcher = dispatcher;
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                await _db.SaveChangesAsync(ct);

                // 1) collect events from tracked aggregates
                var domainEntities = _db.ChangeTracker.Entries<IHasDomainEvents>()
                    .Select(e => e.Entity)
                    .ToList();

                var events = domainEntities.SelectMany(e => e.DomainEvents).ToList();

                // 2) dispatch
                await _dispatcher.DispatchAsync(events, ct);

                // 3) clear events
                foreach (var entity in domainEntities)
                    entity.ClearDomainEvents();

                // 4) persist side effects (ProcessedEvents, etc.)
                await _db.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);
            }
            catch
            {
                try { await tx.RollbackAsync(ct); } catch { }
                throw;
            }
        }
    }
}
