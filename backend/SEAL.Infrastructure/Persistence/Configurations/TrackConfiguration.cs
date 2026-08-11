using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class TrackConfiguration : IEntityTypeConfiguration<Track>
    {
        public void Configure(EntityTypeBuilder<Track> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TrackName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.SubmissionRuleDescription).HasMaxLength(2000);
            
            builder.HasOne(t => t.Template)
                   .WithMany(tp => tp.Tracks)
                   .HasForeignKey(t => t.TemplateId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
