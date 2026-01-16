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
            return await _db.SaveChangesAsync(ct);
        }
    }
}
