using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence
{
    public sealed class RecuriaDbContextFactory : IDesignTimeDbContextFactory<RecuriaDbContext>
    {
        public RecuriaDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<RecuriaDbContext>()
                .UseSqlServer(
                    "Server=(localdb)\\mssqllocaldb;Database=Recuria;Trusted_Connection=True;MultipleActiveResultSets=true")
                .Options;

            return new RecuriaDbContext(options);
        }
    }
}
