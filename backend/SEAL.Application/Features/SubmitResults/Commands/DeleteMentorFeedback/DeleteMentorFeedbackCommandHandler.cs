using MediatR;
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
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            var feedback = await _unitOfWork.GetRepository<MentorFeedback>().GetByIdAsync(request.FeedbackId);
            if (feedback == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Nhận xét có ID '{request.FeedbackId}' không tồn tại.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isOwner = feedback.MentorId == currentUserId;

            if (!isAdmin && !isOwner)
            {
                return new BaseException.ForbiddenException("Chỉ Cố vấn đã viết nhận xét này (hoặc Admin) mới được xóa.");
            }

            await _unitOfWork.GetRepository<MentorFeedback>().DeleteAsync(feedback);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
