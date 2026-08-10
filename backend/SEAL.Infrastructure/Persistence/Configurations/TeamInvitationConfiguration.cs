using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class TeamInvitationConfiguration : IEntityTypeConfiguration<TeamInvitation>
    {
        public void Configure(EntityTypeBuilder<TeamInvitation> builder)
        {
            builder.ToTable("TeamInvitations");
            builder.HasKey(x => x.Id);

            // Lưu enum dưới dạng string cho dễ đọc trong DB
            builder.Property(x => x.Status)
                   .HasConversion<string>()
                   .IsRequired()
                   .HasMaxLength(30);

            builder.Property(x => x.Notes).HasMaxLength(500);

            // N TeamInvitation - 1 Team (xóa team thì xóa luôn lời mời)
            builder.HasOne(x => x.Team)
                   .WithMany()
                   .HasForeignKey(x => x.TeamId)
                   .OnDelete(DeleteBehavior.Cascade);

            // N TeamInvitation - 1 User (người được mời) - Restrict để tránh nhiều đường cascade
            builder.HasOne(x => x.InvitedUser)
                   .WithMany()
                   .HasForeignKey(x => x.InvitedUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
