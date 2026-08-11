using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class CriteriaConfiguration : IEntityTypeConfiguration<Criteria>
    {
        public void Configure(EntityTypeBuilder<Criteria> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CriteriaName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.Description).HasMaxLength(1000);
        }
    }
}
