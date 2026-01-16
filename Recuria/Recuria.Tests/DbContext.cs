using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Recuria.Infrastructure.Persistence;

namespace Recuria.Tests
{
    public static RecuriaDbContext BuildInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RecuriaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new RecuriaDbContext(options);
    }
}
