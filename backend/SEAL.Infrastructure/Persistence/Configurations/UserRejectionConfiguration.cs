using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class UserRejectionConfiguration : IEntityTypeConfiguration<UserRejection>
    {
        public void Configure(EntityTypeBuilder<UserRejection> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Reason).HasMaxLength(1000);
            
            builder.HasOne(x => x.User)
                   .WithMany(x => x.UserRejections)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
