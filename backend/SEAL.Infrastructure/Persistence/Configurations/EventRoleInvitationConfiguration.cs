using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class EventRoleInvitationConfiguration : IEntityTypeConfiguration<EventRoleInvitation>
    {
        public void Configure(EntityTypeBuilder<EventRoleInvitation> builder)
        {
            builder.ToTable("EventRoleInvitations");
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.InvitedUserId);
            builder.HasIndex(x => new { x.EventId, x.Status });

            // Lưu enum dưới dạng string cho dễ đọc trong DB
            builder.Property(x => x.RoleName)
                   .HasConversion<string>()
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.Status)
                   .HasConversion<string>()
                   .IsRequired()
                   .HasMaxLength(30);

            builder.Property(x => x.Notes).HasMaxLength(500);

            // N EventRoleInvitation - 1 Event (xóa event thì xóa luôn lời mời)
            builder.HasOne(x => x.Event)
                   .WithMany()
                   .HasForeignKey(x => x.EventId)
                   .OnDelete(DeleteBehavior.Cascade);

            // N EventRoleInvitation - 1 User (người được mời) - Restrict để tránh nhiều đường cascade
            builder.HasOne(x => x.InvitedUser)
                   .WithMany()
                   .HasForeignKey(x => x.InvitedUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // N EventRoleInvitation - 1 User (người gửi mời) - Restrict để tránh nhiều đường cascade
            builder.HasOne(x => x.InvitedByUser)
                   .WithMany()
                   .HasForeignKey(x => x.InvitedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // N EventRoleInvitation - 1 Track (tùy chọn)
            builder.HasOne(x => x.Track)
                   .WithMany()
                   .HasForeignKey(x => x.TrackId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
