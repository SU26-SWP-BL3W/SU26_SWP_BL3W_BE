using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.SubmitResults.Commands.DeleteMentorFeedback
{
    public class DeleteMentorFeedbackCommandHandler : IRequestHandler<DeleteMentorFeedbackCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public DeleteMentorFeedbackCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(DeleteMentorFeedbackCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            var feedback = await _unitOfWork.GetRepository<MentorFeedback>().Entities
                .Include(f => f.EventRole)
                .FirstOrDefaultAsync(f => f.Id == request.FeedbackId, cancellationToken);

            if (feedback == null)
            {
                return BaseException.BadRequestNotFoundResponse("Không tìm thấy nhận xét này.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isAuthor = feedback.EventRole?.UserId == currentUserId;

            if (!isAdmin && !isAuthor)
            {
                return new BaseException.ForbiddenException("Chỉ người viết nhận xét hoặc Quản trị viên mới có quyền xóa.");
            }

            await _unitOfWork.GetRepository<MentorFeedback>().DeleteAsync(feedback);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
