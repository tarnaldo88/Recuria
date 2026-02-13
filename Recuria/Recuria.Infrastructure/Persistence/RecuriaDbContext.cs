using Microsoft.EntityFrameworkCore;
using Recuria.Application;
using Recuria.Domain;
using Recuria.Domain.Abstractions;
using Recuria.Domain.Entities;
using Recuria.Infrastructure.Outbox;
using Recuria.Infrastructure.Persistence.Entities;
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
        public DbSet<OutBoxMessage> OutBoxMessages => Set<OutBoxMessage>();
        public DbSet<BillingAttempt> BillingAttempts => Set<BillingAttempt>();
        public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
        public DbSet<ApiIdempotencyRecord> ApiIdempotencyRecords => Set<ApiIdempotencyRecord>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecuriaDbContext).Assembly);
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Subscription)
                .WithMany()
                .HasForeignKey(i => i.SubscriptionId);

            //base.OnModelCreating(modelBuilder);

            //modelBuilder.Entity<ProcessedEvent>()
            //    .HasIndex(x => new { x.EventId, x.Handler })
            //    .IsUnique();
        }

        //public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        //{
        //    var domainEvents = ChangeTracker
        //        .Entries<IHasDomainEvents>()
        //        .SelectMany(e => e.Entity.DomainEvents)
        //        .ToList();

        //    var outBoxMessages = domainEvents
        //        .Select(OutBoxMessage.FromDomainEvent)
        //        .ToList();

        //    await base.SaveChangesAsync(cancellationToken);

        //    if (outBoxMessages.Any())
        //    {
        //        OutBoxMessages.AddRange(outBoxMessages);
        //        await base.SaveChangesAsync(cancellationToken);
        //    }

        //    foreach (var entity in ChangeTracker.Entries<IHasDomainEvents>())
        //    {
        //        //entity.Entity.ClearDomainEvents();
        //    }

        //    return 1;
        //}
    }
}
