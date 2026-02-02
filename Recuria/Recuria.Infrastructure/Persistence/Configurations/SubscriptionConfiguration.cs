using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recuria.Domain.Entities;

namespace Recuria.Infrastructure.Persistence.Configurations
{
    public class SubscriptionConfiguration
        : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.HasKey(s => s.Id);

            builder
                .HasOne(s => s.Organization)
                .WithMany(o => o.Subscriptions)
                .HasForeignKey(s => s.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(s => s.Plan).IsRequired();
            builder.Property(s => s.Status).IsRequired();
            builder.Property(s => s.PeriodStart).IsRequired();
            builder.Property(s => s.PeriodEnd).IsRequired();
            builder.Property(s => s.RowVersion).IsRowVersion();

            builder.HasIndex(s => s.OrganizationId);
            builder.HasIndex(s => new { s.Status, s.PeriodEnd });
        }
    }
}
