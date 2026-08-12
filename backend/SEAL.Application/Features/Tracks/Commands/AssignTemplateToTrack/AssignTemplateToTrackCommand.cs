using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Tracks.Commands.AssignTemplateToTrack.Models;

namespace SEAL_Application.Features.Tracks.Commands.AssignTemplateToTrack
{
    public class AssignTemplateToTrackCommand : IRequest<Result<bool>>
    {
        public string TrackId { get; set; } = string.Empty;
        public AssignTemplateToTrackRequestModel Model { get; set; } = null!;
    }
}

