using Microsoft.EntityFrameworkCore;
using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recuria.Application;

namespace Recuria.Infrastructure.Persistence
{
    public class RecuriaDbContext : DbContext
    {
        public RecuriaDbContext(DbContextOptions<RecuriaDbContext> options) : base(options)
        {
        }

        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<Invoice> Invoices => Set<Invoice>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(RecuriaDbContext).Assembly
            );
        }
    }
}
