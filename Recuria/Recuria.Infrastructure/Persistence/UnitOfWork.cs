using Microsoft.EntityFrameworkCore;
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
            if (_db.Database.IsInMemory())
            {
                await CommitCoreAsync(ct);
                return;
            }

            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                await CommitCoreAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                try { await tx.RollbackAsync(ct); } catch { }
                throw;
            }
        }

        private async Task CommitCoreAsync(CancellationToken ct)
        {
            // 1) collect entities + events BEFORE SaveChanges (critical)
            var domainEntities = _db.ChangeTracker.Entries<IHasDomainEvents>()
                .Select(e => e.Entity)
                .ToList();

            var events = domainEntities
                .SelectMany(e => e.DomainEvents)
                .ToList();

            // 2) persist aggregate changes
            await _db.SaveChangesAsync(ct);

            // 3) dispatch domain events (handlers mark processed etc.)
            await _dispatcher.DispatchAsync(events, ct);

            // 4) clear domain events after dispatch
            foreach (var entity in domainEntities)
                entity.ClearDomainEvents();

            // 5) persist side effects if handler used SAME DbContext instance
            // (harmless even if handler saved separately)
            await _db.SaveChangesAsync(ct);
        }
    }
}
