using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recuria.Domain.Entities;

namespace Recuria.Infrastructure.Persistence.Configurations
{
    public sealed class StripeWebhookEventConfiguration : IEntityTypeConfiguration<StripeWebhookEvent>
    {
        public void Configure(EntityTypeBuilder<StripeWebhookEvent> b)
        {
            b.ToTable("StripeWebhookEvents");
            b.HasKey(x => x.Id);

            b.Property(x => x.StripeEventId).HasMaxLength(128).IsRequired();
            b.Property(x => x.EventType).HasMaxLength(128).IsRequired();

            b.HasIndex(x => x.StripeEventId).IsUnique();
        }
    }
}
