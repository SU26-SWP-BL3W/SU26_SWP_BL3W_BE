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
        }
    }
}