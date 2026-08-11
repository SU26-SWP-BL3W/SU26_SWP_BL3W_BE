using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EventName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.Season).HasMaxLength(100);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.PhotoEventUrl).HasMaxLength(500);
            builder.Property(x => x.MaxTeams).IsRequired();

            // 1 Event - N Rounds
            builder.HasMany(x => x.Rounds)
                   .WithOne(x => x.Event)
                   .HasForeignKey(x => x.EventId)
                   .OnDelete(DeleteBehavior.Cascade);

            // 1 Event - N Prizes
            builder.HasMany(x => x.Prizes)
                   .WithOne(x => x.Event)
                   .HasForeignKey(x => x.EventId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
