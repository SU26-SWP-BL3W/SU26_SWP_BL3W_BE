using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Mentors.Commands.InviteMentorToTrack.Models;

namespace SEAL_Application.Features.Mentors.Commands.InviteMentorToTrack
{
    public class InviteMentorToTrackCommand : IRequest<Result<InviteMentorToTrackResponseModel>>
    {
        public InviteMentorToTrackRequestModel Model { get; set; } = new InviteMentorToTrackRequestModel();
    }
}

