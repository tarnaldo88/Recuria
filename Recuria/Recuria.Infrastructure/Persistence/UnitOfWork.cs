using Recuria.Application.Interface.Abstractions;
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

        public UnitOfWork(RecuriaDbContext db)
        {
            _db = db;
        }

        public async Task<int> CommitAsync(CancellationToken ct = default)
        {
            using var tx = await _db.Database.BeginTransactionAsync(ct);

            try
            {
                var result = await _db.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);

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
