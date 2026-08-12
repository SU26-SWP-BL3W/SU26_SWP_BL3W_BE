using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Criterias.Commands.DeleteCriteria
{
    public class DeleteCriteriaCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; }

        public DeleteCriteriaCommand(string id)
        {
            Id = id;
        }
    }
}

