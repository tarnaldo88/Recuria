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
    public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.Name).IsRequired().HasMaxLength(200);

            builder.HasMany(o => o.Users).WithOne(u => u.Organization!).HasForeignKey(u => u.Organization.Id);

            builder.HasOne(o => o.CurrentSubscription).WithOne().HasForeignKey<Subscription>(s => s.OrganizationId);
        }
    }
}
