using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.EventRoles.Commands.RemoveEventRole
{
    public class RemoveEventRoleCommandHandler : IRequestHandler<RemoveEventRoleCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public RemoveEventRoleCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(RemoveEventRoleCommand request, CancellationToken cancellationToken)
        {
            var eventRole = await _unitOfWork.GetRepository<EventRole>().GetByIdAsync(request.Id);
            if (eventRole == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Vai trò có ID '{request.Id}' không tồn tại.");
            }

            // 1. Vai trò THÀNH VIÊN ĐỘI không được xóa qua endpoint này — phải đi qua luồng đội
            //    (rời đội / xóa thành viên / chuyển quyền) nơi có đủ rào: đội đã chốt, không đụng leader,
            //    tối thiểu thành viên... Xóa thẳng ở đây là cửa sau phá toàn bộ ràng buộc đội.
            if (eventRole.RoleName == EventRoleType.TeamLeader || eventRole.RoleName == EventRoleType.TeamMember)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Vai trò thành viên đội không thể xóa trực tiếp. Vui lòng dùng chức năng rời đội/xóa thành viên/chuyển quyền của đội.");
            }

            // 2. Vai trò ĐÃ CHẤM ĐIỂM không được xóa: FK Score -> EventRole là Cascade,
            //    xóa role sẽ NUỐT toàn bộ phiếu điểm im lặng -> kết quả lệch khỏi điểm gốc.
            var hasScores = await _unitOfWork.GetRepository<Score>().AnyAsync(
                s => s.EventRoleId == eventRole.Id, cancellationToken);
            if (hasScores)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Vai trò này đã có phiếu chấm điểm nên không thể xóa (xóa sẽ mất toàn bộ điểm đã chấm).");
            }

            // Xóa cứng
            await _unitOfWork.GetRepository<EventRole>().DeleteAsync(eventRole);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

