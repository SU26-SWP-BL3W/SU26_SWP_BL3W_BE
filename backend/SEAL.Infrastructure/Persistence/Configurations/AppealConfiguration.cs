using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class AppealConfiguration : IEntityTypeConfiguration<Appeal>
    {
        public void Configure(EntityTypeBuilder<Appeal> builder)
        {
            builder.ToTable("Appeals");
            
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TeamId)
                .IsRequired();

            builder.Property(x => x.SubmitResultId)
                .IsRequired();

            builder.Property(x => x.Reason)
                .IsRequired()
                .HasColumnType("text"); // allow long reason

            builder.Property(x => x.Response)
                .HasColumnType("text");

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<string>(); // Save as string

            // Relationships
            builder.HasOne(x => x.Team)
                .WithMany()
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SubmitResult)
                .WithMany()
                .HasForeignKey(x => x.SubmitResultId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
