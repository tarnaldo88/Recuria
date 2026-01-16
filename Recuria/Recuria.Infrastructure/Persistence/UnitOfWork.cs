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

        public async Task<int> CommitAsync(CancellationToken ct = default)
        {
            using var tx = await _db.Database.BeginTransactionAsync(ct);

            try
            {
                var result = await _db.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);
                var events = _db.ChangeTracker
                    .Entries<Entity>()
                    .SelectMany(e => e.Entity.DomainEvents)
                    .ToList();

                foreach (var entity in _db.ChangeTracker.Entries<Entity>())
                {
                    entity.Entity.ClearDomainEvents();
                }

                await _dispatcher.DispatchAsync(events, ct);

                return result;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }
}
