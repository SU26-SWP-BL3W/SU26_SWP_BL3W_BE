using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.SubmitResults.Commands.CreateMentorFeedback;
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

        public GetMentorFeedbacksQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<MentorFeedbackModel>>> Handle(GetMentorFeedbacksQuery request, CancellationToken cancellationToken)
        {
            var feedbacks = await _unitOfWork.GetRepository<MentorFeedback>().GetQueryable()
                .Where(f => f.SubmitResultId == request.SubmitResultId)
                .Include(f => f.Mentor)
                .OrderBy(f => f.CreatedTime)
                .Select(f => new MentorFeedbackModel
                {
                    Id = f.Id,
                    SubmitResultId = f.SubmitResultId,
                    MentorId = f.MentorId,
                    MentorName = f.Mentor != null ? f.Mentor.FullName : null,
                    Comment = f.Comment,
                    CreatedTime = f.CreatedTime
                })
                .ToListAsync(cancellationToken);

            return feedbacks;
        }
    }
}
