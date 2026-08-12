namespace SEAL_Application.Features.Judges.Commands.InviteJudgeToTrack.Models
{
    /// <summary>
    /// Mời 1 giám khảo chấm đúng MỘT hạng mục (Track) của sự kiện.
    /// Nếu email chưa có tài khoản -> tạo tài khoản tạm chỉ dùng trong vòng đời sự kiện.
    /// </summary>
    public class InviteJudgeToTrackRequestModel
    {
        public string EventId { get; set; } = string.Empty;
        public string TrackId { get; set; } = string.Empty;
        public string JudgeEmail { get; set; } = string.Empty;
        public string JudgeFullName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
