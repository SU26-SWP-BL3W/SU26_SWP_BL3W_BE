using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChangePasswordCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.GetRepository<User>().Entities
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                return BaseException.BadRequestNotFoundResponse("Không tìm thấy thông tin người dùng.");
            }

            var hashedOldPassword = FixedSaltPasswordHasher.HashPassword(request.Model.OldPassword);
            if (user.PasswordHash != hashedOldPassword)
            {
                return BaseException.BadRequestInvaildInputResponse("Mật khẩu cũ không chính xác.");
            }

            user.PasswordHash = FixedSaltPasswordHasher.HashPassword(request.Model.NewPassword);
            user.MustChangePassword = false;

            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

