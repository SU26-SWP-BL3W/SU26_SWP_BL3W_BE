namespace SEAL_Application.Features.FinalResults.Commands.CreateFinalResult.Models
{
    public class CreateFinalResultRequestModel
    {
        public string TeamId { get; set; } = string.Empty;

        // Nhập ĐÚNG MỘT trong 3 để xác định phạm vi kết quả:
        // RoundId = kết quả 1 vòng | EventId = kết quả toàn sự kiện | TrackId = kết quả 1 hạng mục.
        public string? RoundId { get; set; }
        public string? EventId { get; set; }
        public string? TrackId { get; set; }

        public decimal FinalScore { get; set; }
        public int Rank { get; set; }
        public bool IsAdvanced { get; set; }
    }
}
