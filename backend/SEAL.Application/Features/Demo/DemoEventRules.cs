namespace SEAL_Application.Features.Demo
{
    /// <summary>
    /// Sự kiện seed "Nộp Bài &amp; Chấm" — demo oral: thí sinh nộp live, giám khảo chấm ngay, EC tính/công bố.
    /// Bỏ qua một số gate thời gian nộp/chấm/công bố so với luồng production.
    /// </summary>
    public static class DemoEventRules
    {
        public const string LiveSubmitScorePrefix = "Nộp Bài & Chấm -";

        public static bool IsLiveSubmitScoreEvent(string? eventName) =>
            !string.IsNullOrWhiteSpace(eventName)
            && eventName.StartsWith(LiveSubmitScorePrefix, System.StringComparison.Ordinal);
    }

}
