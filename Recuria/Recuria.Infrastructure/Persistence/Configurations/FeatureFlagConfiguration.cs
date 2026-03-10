using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recuria.Domain;

namespace Recuria.Infrastructure.Persistence.Configurations
{
    public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
    {
        public void Configure(EntityTypeBuilder<FeatureFlag> builder)
        {
            builder.HasKey(f => f.Id);
            builder.HasIndex(f => f.Name).IsUnique();
            builder.Property(f => f.Name).HasMaxLength(100).IsRequired();
            builder.Property(f => f.Description).HasMaxLength(500);
            builder.Property(f => f.EnabledFor).HasMaxLength(2000);
            builder.Property(f => f.Environment).HasMaxLength(50);
            builder.Property(f => f.ModifiedBy).HasMaxLength(100);
        }
    }
}