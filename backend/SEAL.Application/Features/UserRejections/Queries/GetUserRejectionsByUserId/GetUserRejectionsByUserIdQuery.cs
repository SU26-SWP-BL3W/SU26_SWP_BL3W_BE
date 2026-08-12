using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.UserRejections.Queries.GetUserRejectionsByUserId.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.UserRejections.Queries.GetUserRejectionsByUserId
{
    public class GetUserRejectionsByUserIdQuery : IRequest<Result<List<UserRejectionModel>>>
    {
        public string UserId { get; set; }

        public GetUserRejectionsByUserIdQuery(string userId)
        {
            UserId = userId;
        }
    }
}

