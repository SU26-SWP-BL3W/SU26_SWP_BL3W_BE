using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Criterias.Commands.UpdateCriteria.Models;

namespace SEAL_Application.Features.Criterias.Commands.UpdateCriteria
{
    public class UpdateCriteriaCommand : IRequest<Result<UpdateCriteriaResponseModel>>
    {
        public string Id { get; set; } = string.Empty;
        public UpdateCriteriaRequestModel Model { get; set; } = new UpdateCriteriaRequestModel();
    }
}

