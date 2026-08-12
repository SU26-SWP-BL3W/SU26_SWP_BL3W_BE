namespace SEAL_Application.Features.Mentors.Commands.InviteMentorToTrack.Models
{
    /// <summary>
    /// Mời 1 mentor tham gia hướng dẫn đúng MỘT hạng mục (Track) của sự kiện.
    /// Nếu email chưa có tài khoản -> tạo tài khoản tạm chỉ dùng trong vòng đời sự kiện.
    /// </summary>
    public class InviteMentorToTrackRequestModel
    {
        public string EventId { get; set; } = string.Empty;
        public string TrackId { get; set; } = string.Empty;
        public string MentorEmail { get; set; } = string.Empty;
        public string MentorFullName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
