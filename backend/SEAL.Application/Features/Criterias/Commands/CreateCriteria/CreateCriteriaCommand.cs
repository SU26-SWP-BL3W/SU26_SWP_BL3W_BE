using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Criterias.Commands.CreateCriteria.Models;

namespace SEAL_Application.Features.Criterias.Commands.CreateCriteria
{
    public class CreateCriteriaCommand : IRequest<Result<CreateCriteriaResponseModel>>
    {
        public CreateCriteriaRequestModel Model { get; set; } = new CreateCriteriaRequestModel();
    }
}

