using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class TemplateCriteriaConfiguration : IEntityTypeConfiguration<TemplateCriteria>
    {
        public void Configure(EntityTypeBuilder<TemplateCriteria> builder)
        {
            builder.HasKey(tc => new { tc.TemplateId, tc.CriteriaId });

            builder.Property(tc => tc.Weight).HasPrecision(18, 2);
            builder.Property(tc => tc.MaxScore).HasPrecision(18, 2);

            builder.HasOne(tc => tc.Template)
                   .WithMany(t => t.TemplateCriterias)
                   .HasForeignKey(tc => tc.TemplateId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(tc => tc.Criteria)
                   .WithMany(c => c.TemplateCriterias)
                   .HasForeignKey(tc => tc.CriteriaId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
