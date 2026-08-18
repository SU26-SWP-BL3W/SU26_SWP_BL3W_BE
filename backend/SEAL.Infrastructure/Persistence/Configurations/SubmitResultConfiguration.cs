using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class SubmitResultConfiguration : IEntityTypeConfiguration<SubmitResult>
    {
        public void Configure(EntityTypeBuilder<SubmitResult> builder)
        {
            builder.ToTable("SubmitResults");
            builder.HasKey(sr => sr.Id);

            builder.Property(sr => sr.SubmissionUrl)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(sr => sr.RepoUrl).HasMaxLength(2000);
            builder.Property(sr => sr.DemoUrl).HasMaxLength(2000);
            builder.Property(sr => sr.SlideUrl).HasMaxLength(2000);
            builder.Property(sr => sr.RepoHost).HasMaxLength(16);
            builder.Property(sr => sr.RepoFullName).HasMaxLength(200);

            builder.Property(sr => sr.Description)
                .HasMaxLength(1000);

            builder.HasOne(sr => sr.Team)
                .WithMany(t => t.SubmitResults)
                .HasForeignKey(sr => sr.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sr => sr.Track)
                .WithMany()
                .HasForeignKey(sr => sr.TrackId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sr => sr.Round)
                .WithMany()
                .HasForeignKey(sr => sr.RoundId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Chặn race condition ở tầng DB: 2 request tạo bài nộp đồng thời cho cùng
            // (Team, Track, Round) có thể cùng vượt qua check AnyAsync ở handler trước khi
            // commit — unique index là lưới an toàn cuối cùng, request thua sẽ nhận lỗi DB.
            builder.HasIndex(sr => new { sr.TeamId, sr.TrackId, sr.RoundId })
                .IsUnique();
        }
    }
}