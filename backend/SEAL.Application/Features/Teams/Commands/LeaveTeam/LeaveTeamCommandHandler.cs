// [FLOW3-DOITHI][LeaveTeam] Thanh vien (khong phai Truong nhom) tu roi doi khi doi con dang lap; chan roi sau khi da chot danh sach.

using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Teams.Commands.LeaveTeam
{
    public class LeaveTeamCommandHandler : IRequestHandler<LeaveTeamCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public LeaveTeamCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(LeaveTeamCommand request, CancellationToken cancellationToken)
        {
            // 0. CurrentUser bắt buộc
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            // 1. Tìm EventRole của currentUser trong team này
            var myRole = await _unitOfWork.GetRepository<EventRole>().Entities
                .FirstOrDefaultAsync(er => er.UserId == currentUserId
                                        && er.TeamId == request.TeamId,
                                     cancellationToken);
            if (myRole == null)
            {
                return BaseException.BadRequestNotFoundResponse("Bạn không phải thành viên của nhóm này.");
            }

            // 2. Chỉ đội đang lập (Forming) mới cho tự rời. Đội đã CHỐT DANH SÁCH (PendingApproval)
            //    hoặc đã đăng ký chính thức (Registered) đều bị khóa — nếu chỉ chặn Registered thì
            //    thành viên vẫn rút được lúc EC đang duyệt, làm đội tụt dưới 3 người mà vẫn được duyệt.
            var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(request.TeamId);
            if (team != null && team.Status != TeamStatus.Forming)
            {
                return BaseException.BadRequestInvaildInputResponse("Đội đã chốt danh sách, bạn không thể tự rời. Vui lòng liên hệ EventCoordinator.");
            }

            // 3. TeamLeader KHÔNG được tự rời (phải Delete Team hoặc transfer Leader)
            if (myRole.RoleName == EventRoleType.TeamLeader)
            {
                return BaseException.BadRequestInvaildInputResponse("TeamLeader không thể tự rời nhóm. Hãy xóa nhóm hoặc chuyển vai trò Leader cho thành viên khác trước.");
            }

            // 3. Xóa cứng EventRole
            await _unitOfWork.GetRepository<EventRole>().DeleteAsync(myRole);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


