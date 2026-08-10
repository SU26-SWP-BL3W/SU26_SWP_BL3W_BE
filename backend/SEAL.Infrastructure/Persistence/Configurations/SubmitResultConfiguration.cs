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
                .HasMaxLength(500);

            builder.Property(sr => sr.Description)
                .HasMaxLength(1000);

            builder.HasOne(sr => sr.Team)
                .WithMany(t => t.SubmitResults)
                .HasForeignKey(sr => sr.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            // Khoá ngoại BẮT BUỘC trỏ đến Track. Vòng thi (Round) được suy ra qua Track.RoundId,
            // nên SubmitResult KHÔNG còn khoá ngoại trực tiếp tới Round (đúng với ERD).
            builder.HasOne(sr => sr.Track)
                .WithMany()
                .HasForeignKey(sr => sr.TrackId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}