using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence.Configurations
{
    public class BillingAttemptConfiguration : IEntityTypeConfiguration<BillingAttempt>
    {
        public void Configure(EntityTypeBuilder<BillingAttempt> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SubscriptionId).IsRequired();

            builder.Property(x => x.Succeeded).IsRequired();

            builder.Property(x => x.FailureReason).HasMaxLength(500);

            builder.Property(x => x.AttemptedAt).IsRequired();
        }
    }
}
