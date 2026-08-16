using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class MentorFeedbackConfiguration : IEntityTypeConfiguration<MentorFeedback>
    {
        public void Configure(EntityTypeBuilder<MentorFeedback> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FeedbackText).IsRequired().HasMaxLength(2000);

            builder.HasOne(x => x.SubmitResult)
                   .WithMany()
                   .HasForeignKey(x => x.SubmitResultId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.EventRole)
                   .WithMany()
                   .HasForeignKey(x => x.EventRoleId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
