using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Email).IsRequired().HasMaxLength(255);
            builder.HasIndex(x => x.Email).IsUnique(); // Email is unique
            
            builder.Property(x => x.StudentCode).HasMaxLength(50);
            builder.Property(x => x.IsFpt).HasDefaultValue(true);
            builder.Property(x => x.PhotoStudentCardUrl).HasMaxLength(500);
            
            builder.Property(x => x.PasswordHash).IsRequired();
            builder.Property(x => x.FullName).IsRequired().HasMaxLength(255);
        }
    }
}
