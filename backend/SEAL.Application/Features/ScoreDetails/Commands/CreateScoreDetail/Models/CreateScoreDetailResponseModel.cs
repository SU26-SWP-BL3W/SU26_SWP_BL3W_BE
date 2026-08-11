using System;

namespace SEAL_Application.Features.ScoreDetails.Commands.CreateScoreDetail.Models
{
    public class CreateScoreDetailResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string ScoreId { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public string CriteriaId { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal ScoreTotal { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
