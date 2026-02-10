using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recuria.Domain;

namespace Recuria.Infrastructure.Persistence.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.HasKey(i => i.Id);

            builder.Property(i => i.InvoiceNumber)
                .HasMaxLength(64);

            builder.Property(i => i.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(i => i.InvoiceDate)
                .IsRequired();

            builder.Property(i => i.PaidOnUtc);

            builder.Property(i => i.Description)
                .HasMaxLength(500);

            builder.HasIndex(i => i.SubscriptionId);
            builder.HasIndex(i => i.InvoiceDate);
        }
    }
}
