using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class ScoreDetailConfiguration : IEntityTypeConfiguration<ScoreDetail>
    {
        public void Configure(EntityTypeBuilder<ScoreDetail> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Value).HasPrecision(18, 2);

            // N ScoreDetail - 1 TemplateCriteria (composite FK)
            builder.HasOne(x => x.TemplateCriteria)
                   .WithMany()
                   .HasForeignKey(x => new { x.TemplateId, x.CriteriaId })
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
