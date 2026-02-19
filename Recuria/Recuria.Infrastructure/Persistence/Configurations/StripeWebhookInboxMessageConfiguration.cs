using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence.Configurations
{
    public sealed class StripeWebhookInboxMessageConfiguration : IEntityTypeConfiguration<StripeWebhookInboxMessage>
    {
        public void Configure(EntityTypeBuilder<StripeWebhookInboxMessage> b)
        {
            b.ToTable("StripeWebhookInboxMessages");
            b.HasKey(x => x.Id);

            b.Property(x => x.StripeEventId).HasMaxLength(128).IsRequired();
            b.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            b.Property(x => x.Payload).IsRequired();
            b.Property(x => x.LastError).HasMaxLength(2000);

            b.HasIndex(x => x.StripeEventId).IsUnique();
            b.HasIndex(x => new { x.ProcessedOnUtc, x.NextAttemptOnUtc });
        }
    }
}
