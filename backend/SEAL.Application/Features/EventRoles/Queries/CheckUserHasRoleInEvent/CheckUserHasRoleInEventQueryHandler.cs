using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.EventRoles.Queries.CheckUserHasRoleInEvent
{
    public class CheckUserHasRoleInEventQueryHandler : IRequestHandler<CheckUserHasRoleInEventQuery, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CheckUserHasRoleInEventQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(CheckUserHasRoleInEventQuery request, CancellationToken cancellationToken)
        {
            var hasRole = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == request.UserId && 
                      er.EventId == request.EventId && 
                      er.RoleName == request.RoleName,
                cancellationToken);

            return hasRole;
        }
    }
}

