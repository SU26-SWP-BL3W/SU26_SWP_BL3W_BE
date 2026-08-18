using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class FptStudentConfiguration : IEntityTypeConfiguration<FptStudent>
    {
        public void Configure(EntityTypeBuilder<FptStudent> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.StudentCode).IsRequired().HasMaxLength(20);
            builder.Property(x => x.FullName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.Email).HasMaxLength(255);
            builder.Property(x => x.Major).HasMaxLength(255);
            builder.Property(x => x.Campus).HasMaxLength(255);
            builder.Property(x => x.Status).IsRequired().HasMaxLength(20);

            builder.HasIndex(x => x.StudentCode).IsUnique();
        }
    }
}
