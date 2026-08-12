using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Users.Models;

namespace SEAL_Application.Features.Users.Queries.GetUserProfile
{
    public class GetUserProfileQuery : IRequest<Result<UserModel>>
    {
    }
}

