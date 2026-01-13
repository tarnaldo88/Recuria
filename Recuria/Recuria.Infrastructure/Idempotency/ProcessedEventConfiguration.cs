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
    internal sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
    {
        public void Configure( EntityTypeBuilder<ProcessedEvent> builder)
        {
            builder.ToTable("ProcessedEvents");
            builder.HasKey(x => x.EventId);

            builder.Property(x => x.ProcessedOnUtc).IsRequired();
        }
    }
}
