using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class SchoolConfiguration : IEntityTypeConfiguration<School>
    {
        public void Configure(EntityTypeBuilder<School> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SchoolName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.Address).HasMaxLength(500);
            
            // 1 School - N Users
            builder.HasMany(x => x.Users)
                   .WithOne(x => x.School)
                   .HasForeignKey(x => x.SchoolId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
