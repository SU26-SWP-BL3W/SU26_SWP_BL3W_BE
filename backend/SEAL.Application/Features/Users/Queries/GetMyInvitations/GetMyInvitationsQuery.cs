using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Users.Queries.GetMyInvitations.Models;

namespace SEAL_Application.Features.Users.Queries.GetMyInvitations
{
    public class GetMyInvitationsQuery : IRequest<Result<MyInvitationsResponseModel>>
    {
    }
}

