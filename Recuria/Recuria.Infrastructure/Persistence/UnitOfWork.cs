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

        //public async Task<int> CommitAsync(CancellationToken ct = default)
        //{
        //    using var tx = await _db.Database.BeginTransactionAsync(ct);

        //    try
        //    {
        //        // 1. COLLECT EVENTS BEFORE SAVING
        //        var domainEvents = _db.ChangeTracker
        //            .Entries<Entity>()
        //            .SelectMany(e => e.Entity.DomainEvents)
        //            .ToList();

        //        // 2. SAVE CHANGES
        //        var result = await _db.SaveChangesAsync(ct);

        //        // 3. COMMIT DB TRANSACTION
        //        await tx.CommitAsync(ct);

        //        // 4. CLEAR EVENTS ONLY AFTER COMMIT
        //        foreach (var entry in _db.ChangeTracker.Entries<Entity>())
        //        {
        //            entry.Entity.ClearDomainEvents();
        //        }

        //        // 5. DISPATCH EVENTS
        //        await _dispatcher.DispatchAsync(domainEvents, ct);

        //        return result;
        //    }
        //    catch
        //    {
        //        await tx.RollbackAsync(ct);
        //        throw;
        //    }
        //}

        public async Task CommitAsync(CancellationToken ct = default)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                // Only rollback if transaction is still usable.
                // If Commit failed, EF may already have completed it; Rollback can throw.
                try { await tx.RollbackAsync(ct); } catch { /* swallow */ }
                throw;
            }
        }
    }
}
