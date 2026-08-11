namespace SEAL_Application.Features.ScoreDetails.Commands.CreateScoreDetail.Models
{
    public class CreateScoreDetailRequestModel
    {
        public string ScoreId { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public string CriteriaId { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}
