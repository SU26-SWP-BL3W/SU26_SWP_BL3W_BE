using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Users.Models;
using System;

namespace SEAL_Application.Features.Users.Queries.GetUserById
{
    public class GetUserByIdQuery : IRequest<Result<UserModel?>>
    {
        public string Id { get; set; } = string.Empty;
        public GetUserByIdQuery(string id)
        {
            Id = id;
        }
    }
}

