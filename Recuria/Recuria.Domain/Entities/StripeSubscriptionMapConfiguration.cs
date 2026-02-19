using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recuria.Domain.Entities;

namespace Recuria.Domain.Entities
{
    public sealed class StripeSubscriptionMapConfiguration : IEntityTypeConfiguration<StripeSubscriptionMap>
    {
        public void Configure(EntityTypeBuilder<StripeSubscriptionMap> b)
        {
            b.ToTable("StripeSubscriptionMaps");
            b.HasKey(x => x.Id);

            b.Property(x => x.StripeCustomerId).HasMaxLength(128).IsRequired();
            b.Property(x => x.StripeSubscriptionId).HasMaxLength(128).IsRequired();

            b.HasIndex(x => x.OrganizationId).IsUnique();
            b.HasIndex(x => x.StripeCustomerId);
            b.HasIndex(x => x.StripeSubscriptionId);
        }
    }
}
