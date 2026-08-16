using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.SubmitResults.Queries.GetMentorFeedbacks.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.SubmitResults.Queries.GetMentorFeedbacks
{
    public class GetMentorFeedbacksQueryHandler : IRequestHandler<GetMentorFeedbacksQuery, Result<List<MentorFeedbackModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetMentorFeedbacksQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<List<MentorFeedbackModel>>> Handle(GetMentorFeedbacksQuery request, CancellationToken cancellationToken)
        {
            var submit = await _unitOfWork.GetRepository<SubmitResult>().GetByIdAsync(request.SubmitResultId);
            if (submit == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Bài nộp có ID '{request.SubmitResultId}' không tồn tại.");
            }

            var feedbacks = await _unitOfWork.GetRepository<MentorFeedback>().Entities
                .AsNoTracking()
                .Include(f => f.EventRole)
                    .ThenInclude(er => er.User)
                .Where(f => f.SubmitResultId == request.SubmitResultId)
                .OrderByDescending(f => f.CreatedTime)
                .ToListAsync(cancellationToken);

            var list = feedbacks.Select(f => new MentorFeedbackModel
            {
                Id = f.Id,
                SubmitResultId = f.SubmitResultId,
                EventRoleId = f.EventRoleId,
                MentorName = f.EventRole?.User?.FullName ?? "Cố vấn",
                MentorEmail = f.EventRole?.User?.Email ?? string.Empty,
                FeedbackText = f.FeedbackText,
                CreatedTime = f.CreatedTime
            }).ToList();

            return list;
        }
    }
}
