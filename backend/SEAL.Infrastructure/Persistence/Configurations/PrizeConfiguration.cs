using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class PrizeConfiguration : IEntityTypeConfiguration<Prize>
    {
        public void Configure(EntityTypeBuilder<Prize> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.PrizeName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.Value).HasMaxLength(500);
            builder.Property(x => x.Quantity).IsRequired();

            // 1 Prize - N FinalResults
            builder.HasMany(x => x.FinalResults)
                   .WithOne(x => x.Prize)
                   .HasForeignKey(x => x.PrizeId)
                   .OnDelete(DeleteBehavior.SetNull); // SetNull so deleting a prize doesn't delete the final result

            // 1 Track - N Prizes (optional: Prize.TrackId có thể null = giải chung toàn sự kiện)
            builder.HasOne(x => x.Track)
                   .WithMany()
                   .HasForeignKey(x => x.TrackId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
