using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class FinalResultConfiguration : IEntityTypeConfiguration<FinalResult>
    {
        public void Configure(EntityTypeBuilder<FinalResult> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TeamId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.FinalScore).HasPrecision(18, 2);
            builder.Property(x => x.IsPublished).HasDefaultValue(false);

            // N FinalResult - 1 Round: WithMany(r => r.FinalResults) để tránh EF sinh khóa ngoại
            // bóng "RoundId1" (lỗi 42703 column f.RoundId1 does not exist khi tính KQ / xem BXH).
            builder.HasOne(x => x.Round)
                   .WithMany(r => r.FinalResults)
                   .HasForeignKey(x => x.RoundId)
                   .OnDelete(DeleteBehavior.Restrict);

            // N FinalResult - 1 Event (kết quả cấp toàn sự kiện) - optional
            builder.HasOne(x => x.Event)
                   .WithMany()
                   .HasForeignKey(x => x.EventId)
                   .OnDelete(DeleteBehavior.Restrict);

            // N FinalResult - 1 Track (kết quả 1 hạng mục) - optional
            builder.HasOne(x => x.Track)
                   .WithMany()
                   .HasForeignKey(x => x.TrackId)
                   .OnDelete(DeleteBehavior.Restrict);

            // N FinalResult - 1 Team
            builder.HasOne(x => x.Team)
                   .WithMany()
                   .HasForeignKey(x => x.TeamId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
