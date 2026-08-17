using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class MentorFeedbackConfiguration : IEntityTypeConfiguration<MentorFeedback>
    {
        public void Configure(EntityTypeBuilder<MentorFeedback> builder)
        {
            builder.ToTable("MentorFeedbacks");
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Comment).IsRequired().HasMaxLength(2000);

            builder.HasOne(f => f.SubmitResult)
                .WithMany()
                .HasForeignKey(f => f.SubmitResultId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.Mentor)
                .WithMany()
                .HasForeignKey(f => f.MentorId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
