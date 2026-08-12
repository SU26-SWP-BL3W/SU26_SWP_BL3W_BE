using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Appeals.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.Appeals.Queries.GetAssignedAppeals
{
    public class GetAssignedAppealsQuery : IRequest<Result<List<AppealModel>>>
    {
        public string EventRoleId { get; set; } = string.Empty;
    }
}

