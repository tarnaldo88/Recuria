using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recuria.Infrastructure.Persistence.Entities;

namespace Recuria.Infrastructure.Idempotency
{
    internal sealed class ApiIdempotencyRecordConfiguration : IEntityTypeConfiguration<ApiIdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<ApiIdempotencyRecord> builder)
        {
            builder.ToTable("ApiIdempotencyRecords");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Operation).HasMaxLength(100).IsRequired();
            builder.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired();
            builder.Property(x => x.RequestHash).HasMaxLength(128).IsRequired();
            builder.Property(x => x.CreatedOnUtc).IsRequired();

            builder.HasIndex(x => new { x.OrganizationId, x.Operation, x.IdempotencyKey })
                .IsUnique();
        }
    }
}
