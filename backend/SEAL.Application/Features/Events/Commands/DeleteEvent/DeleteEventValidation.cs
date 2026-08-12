using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Events.Commands.DeleteEvent
{
    public static class DeleteEventValidation
    {
        public static async Task<Result> ValidateDeleteAsync(
            IUnitOfWork unitOfWork,
            Event eventEntity,
            CancellationToken cancellationToken)
        {

            // 2. Kiểm tra đã có đội thi đăng ký (teamCount > 0)
            bool hasTeams = await unitOfWork.GetRepository<Team>().AnyAsync(t => t.EventId == eventEntity.Id, cancellationToken);
            if (hasTeams)
            {
                return Result.Failure(BaseException.BadRequestInvaildInputResponse("Không thể xóa sự kiện đã có đội thi đăng ký để bảo vệ thông tin đăng ký của sinh viên."));
            }

            // 3. Kiểm tra sự kiện đang công khai (status = true)
            if (eventEntity.Status)
            {
                return Result.Failure(BaseException.BadRequestInvaildInputResponse("Sự kiện đang ở trạng thái công khai. Vui lòng chuyển sự kiện về trạng thái Ẩn trước khi xóa."));
            }
            
            return Result.Success();
        }
    }
}

