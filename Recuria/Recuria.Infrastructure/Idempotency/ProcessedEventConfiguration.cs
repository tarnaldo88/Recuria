using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Recuria.Infrastructure.Idempotency
{
    internal sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
    {
        public ProcessedEventConfiguration( EntityTypeBuilder<ProcessedEvent> builder)
        {
            builder.ToTable("ProcessedEvents");
            builder.HasKey(x => x.EventId);

            builder.Property(x => x.ProcessedOnUtc).IsRequired();
        }

        public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
        {
            throw new NotImplementedException();
        }
    }
}
