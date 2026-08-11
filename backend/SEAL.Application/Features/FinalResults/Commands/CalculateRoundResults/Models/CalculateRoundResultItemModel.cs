namespace SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults.Models
{
    public class CalculateRoundResultItemModel
    {
        public string FinalResultId { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string TrackId { get; set; } = string.Empty;
        public decimal FinalScore { get; set; }
        public int Rank { get; set; }
        public bool IsAdvanced { get; set; }
    }

    /// <summary>1 hạng mục trong vòng bị bỏ qua khi tính kết quả vì chưa đủ giám khảo chấm xong.</summary>
    public class SkippedTrackModel
    {
        public string TrackId { get; set; } = string.Empty;
        public string TrackName { get; set; } = string.Empty;
        public int MissingScoreCount { get; set; }
    }

    public class CalculateRoundResultsResponseModel
    {
        /// <summary>Kết quả các hạng mục ĐÃ đủ điều kiện tính (mọi giám khảo được phân đã chấm xong).</summary>
        public System.Collections.Generic.List<CalculateRoundResultItemModel> Results { get; set; } = new();

        /// <summary>Hạng mục CHƯA tính được vì còn thiếu lượt chấm — không chặn các hạng mục khác trong cùng vòng.</summary>
        public System.Collections.Generic.List<SkippedTrackModel> SkippedTracks { get; set; } = new();
    }
}
