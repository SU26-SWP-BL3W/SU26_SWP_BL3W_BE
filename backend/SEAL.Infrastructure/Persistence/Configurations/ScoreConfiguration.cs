using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class ScoreConfiguration : IEntityTypeConfiguration<Score>
    {
        public void Configure(EntityTypeBuilder<Score> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SubmitResultId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.TotalScore).HasPrecision(18, 2);
            builder.Property(x => x.Comment).HasMaxLength(1000);

            // 1 giám khảo (EventRole) chỉ có 1 phiếu chấm cho 1 bài nộp — chặn race 2 request
            // SaveScore đồng thời (double-click/2 tab) cùng tạo 2 Score trùng, làm lệch điểm
            // trung bình khi tính kết quả vòng thi.
            builder.HasIndex(x => new { x.EventRoleId, x.SubmitResultId }).IsUnique();

            // N Score - 1 EventRole (giám khảo)
            builder.HasOne(x => x.EventRole)
                   .WithMany()
                   .HasForeignKey(x => x.EventRoleId)
                   .OnDelete(DeleteBehavior.Cascade);

            // N Score - 1 SubmitResult (bài nộp được chấm) - khớp ERD "Submit_Result 1-N Score (SUBMIT)"
            // Restrict để tránh nhiều đường cascade về Event (Score đã cascade từ EventRole; SubmitResult cascade từ Team/Round)
            builder.HasOne(x => x.SubmitResult)
                   .WithMany()
                   .HasForeignKey(x => x.SubmitResultId)
                   .OnDelete(DeleteBehavior.Restrict);

            // 1 Score - N ScoreDetail
            builder.HasMany(x => x.ScoreDetails)
                   .WithOne(x => x.Score)
                   .HasForeignKey(x => x.ScoreId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
