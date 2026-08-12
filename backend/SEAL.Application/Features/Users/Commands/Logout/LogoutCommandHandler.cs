using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;
using SEAL_Domain.Base;
using Microsoft.EntityFrameworkCore;

namespace SEAL_Application.Features.Users.Commands.Logout
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public LogoutCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.GetRepository<User>().GetQueryable()
                .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                return BaseException.BadRequestNotFoundResponse("User not found");
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            _unitOfWork.GetRepository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

