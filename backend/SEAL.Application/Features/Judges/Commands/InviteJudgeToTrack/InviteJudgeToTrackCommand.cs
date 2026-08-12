using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Judges.Commands.InviteJudgeToTrack.Models;

namespace SEAL_Application.Features.Judges.Commands.InviteJudgeToTrack
{
    public class InviteJudgeToTrackCommand : IRequest<Result<InviteJudgeToTrackResponseModel>>
    {
        public InviteJudgeToTrackRequestModel Model { get; set; } = new InviteJudgeToTrackRequestModel();
    }
}

