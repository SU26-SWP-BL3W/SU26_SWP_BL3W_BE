using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class RoundConfiguration : IEntityTypeConfiguration<Round>
    {
        public void Configure(EntityTypeBuilder<Round> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.RoundName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.AdvancementRule).HasMaxLength(1000);
            
            // 1 Round - N Tracks
            builder.HasMany(x => x.Tracks)
                   .WithOne(x => x.Round)
                   .HasForeignKey(x => x.RoundId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
