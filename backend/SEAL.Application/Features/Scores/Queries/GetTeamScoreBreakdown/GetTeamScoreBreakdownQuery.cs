using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Scores.Queries.GetTeamScoreBreakdown.Models;

namespace SEAL_Application.Features.Scores.Queries.GetTeamScoreBreakdown
{
    /// <summary>
    /// Thí sinh (thành viên đội) xem breakdown điểm theo từng tiêu chí + từng giám khảo cho đội mình.
    /// Giám khảo hiện danh tính với thí sinh; ngược lại giám khảo không thấy đội khi chấm (ẩn danh phía chấm).
    /// </summary>
    public class GetTeamScoreBreakdownQuery : IRequest<Result<TeamScoreBreakdownModel>>
    {
        public string TeamId { get; set; } = string.Empty;

        public GetTeamScoreBreakdownQuery(string teamId)
        {
            TeamId = teamId;
        }
    }
}

