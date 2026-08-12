namespace SEAL_Application.Features.FinalResults.Commands.SetRoundResultsPublishStatus.Models
{
    /// <summary>Body cho PUT /api/FinalResults/round/{roundId}/publish-status.</summary>
    public class SetRoundResultsPublishStatusRequestModel
    {
        /// <summary>true = công bố kết quả; false = thu hồi về nháp.</summary>
        public bool IsPublished { get; set; }
    }
}
