using Microsoft.EntityFrameworkCore;
using Recuria.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(320);

            builder.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(u => u.Role)
                .IsRequired();
            builder.Property(u => u.OrganizationId)
                .IsRequired(false);

            builder.Property(u => u.PasswordHash)
                .HasMaxLength(256)
                .IsRequired(false);

            builder.Property(u => u.PasswordSalt)
                .HasMaxLength(128)
                .IsRequired(false);

            builder.Property(u => u.TokenVersion)
                .HasDefaultValue(0)
                .IsRequired();

            builder.HasIndex(u => new { u.OrganizationId, u.Email })
                .IsUnique();

            builder.HasIndex(u => u.OrganizationId);

            builder.HasOne(u => u.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(u => u.OrganizationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
