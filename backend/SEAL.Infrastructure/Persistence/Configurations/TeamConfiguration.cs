using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class TeamConfiguration : IEntityTypeConfiguration<Team>
    {
        public void Configure(EntityTypeBuilder<Team> builder)
        {
            builder.ToTable("Teams");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(t => t.Description)
                .HasMaxLength(500);

            builder.HasOne(t => t.Event)
                .WithMany()
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.Track)
                .WithMany(tr => tr.Teams)
                .HasForeignKey(t => t.TrackId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(t => t.SubmitResults)
                .WithOne(sr => sr.Team)
                .HasForeignKey(sr => sr.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}