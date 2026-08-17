using MediatR;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.SubmitResults.Commands.CreateMentorFeedback
{
    public class CreateMentorFeedbackCommandHandler : IRequestHandler<CreateMentorFeedbackCommand, Result<MentorFeedbackModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public CreateMentorFeedbackCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<MentorFeedbackModel>> Handle(CreateMentorFeedbackCommand request, CancellationToken cancellationToken)
        {
            var mentorId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(mentorId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            var submitResult = await _unitOfWork.GetRepository<SubmitResult>().GetByIdAsync(request.SubmitResultId);
            if (submitResult == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Bài nộp có ID '{request.SubmitResultId}' không tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(request.Comment))
            {
                return BaseException.BadRequestInvaildInputResponse("Nội dung nhận xét không được để trống.");
            }

            var mentor = await _unitOfWork.GetRepository<User>().GetByIdAsync(mentorId);

            var feedback = new MentorFeedback
            {
                SubmitResultId = request.SubmitResultId,
                MentorId = mentorId,
                Comment = request.Comment.Trim()
            };
            await _unitOfWork.GetRepository<MentorFeedback>().AddAsync(feedback);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new MentorFeedbackModel
            {
                Id = feedback.Id,
                SubmitResultId = feedback.SubmitResultId,
                MentorId = feedback.MentorId,
                MentorName = mentor?.FullName,
                Comment = feedback.Comment,
                CreatedTime = feedback.CreatedTime
            };
        }
    }
}
