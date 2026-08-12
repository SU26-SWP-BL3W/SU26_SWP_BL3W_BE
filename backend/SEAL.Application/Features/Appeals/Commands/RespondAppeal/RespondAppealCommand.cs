using SEAL_Domain.Base;
using MediatR;
using SEAL_Domain.Entity.Enums;

namespace SEAL_Application.Features.Appeals.Commands.RespondAppeal
{
    public class RespondAppealCommand : IRequest<Result<bool>>
    {
        public string AppealId { get; set; } = string.Empty;

        public AppealStatus Status { get; set; }

        public string Response { get; set; } = string.Empty;
        
        public string? AssignedJudgeId { get; set; }
    }
}

