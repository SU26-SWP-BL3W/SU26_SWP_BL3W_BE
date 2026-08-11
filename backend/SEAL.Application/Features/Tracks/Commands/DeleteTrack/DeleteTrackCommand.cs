using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Tracks.Commands.DeleteTrack
{
    public class DeleteTrackCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; }

        public DeleteTrackCommand(string id)
        {
            Id = id;
        }
    }
}

