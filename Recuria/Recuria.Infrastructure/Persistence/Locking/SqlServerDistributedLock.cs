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
    }
}
