using Microsoft.EntityFrameworkCore;
using Recuria.Application;
using Recuria.Domain;
using Recuria.Domain.Abstractions;
using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecuriaDbContext).Assembly);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var domainEvents = ChangeTracker
                .Entries<Entity>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            var result = await base.SaveChangesAsync(cancellationToken);

            foreach (var domainEvent in domainEvents)
            {
                await _domainEventDispatcher.DispatchAsync(domainEvent, cancellationToken);
            }

            foreach (var entity in ChangeTracker.Entries<Entity>())
            {
                entity.Entity.ClearDomainEvents();
            }

            return result;
        }
    }
}
