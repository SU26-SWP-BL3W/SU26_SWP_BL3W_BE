using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Tracks.Models;

namespace SEAL_Application.Features.Tracks.Queries.GetTrackById
{
    public class GetTrackByIdQuery : IRequest<Result<TrackModel?>>
    {
        public string Id { get; set; }

        public GetTrackByIdQuery(string id)
        {
            Id = id;
        }
    }
}

