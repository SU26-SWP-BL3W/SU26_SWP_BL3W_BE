using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultById.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultById
{
    public class GetSubmitResultByIdQuery : IRequest<Result<SubmitResultDetailResponseModel>>
    {
        public string Id { get; set; }

        public GetSubmitResultByIdQuery(string id)
        {
            Id = id;
        }
    }
}
