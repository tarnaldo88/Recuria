using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence.Locking
{
    public sealed class SqlServerDistributedLock : IDatabaseDistributedLock
    {
        private readonly RecuriaDbContext _db;

        public SqlServerDistributedLock(RecuriaDbContext db)
        {
            _db = db;
        }

        public async Task<bool> TryAcquireAsync(string lockName, CancellationToken ct)
        {
            var result = await _db.Database.ExecuteSqlRawAsync(
                """
            DECLARE @result int;
            EXEC @result = sp_getapplock 
                @Resource = {0},
                @LockMode = 'Exclusive',
                @LockTimeout = 0,
                @LockOwner = 'Session';
            SELECT @result;
            """,
                parameters: new[] { lockName },
                cancellationToken: ct
            );

            // result >= 0 = lock acquired
            return result >= 0;
        }

        public async Task ReleaseAsync(string lockName, CancellationToken ct)
        {
            await _db.Database.ExecuteSqlRawAsync(
                """
            EXEC sp_releaseapplock 
                @Resource = {0},
                @LockOwner = 'Session';
            """,
                parameters: new[] { lockName },
                cancellationToken: ct
            );
        }
    }
}
