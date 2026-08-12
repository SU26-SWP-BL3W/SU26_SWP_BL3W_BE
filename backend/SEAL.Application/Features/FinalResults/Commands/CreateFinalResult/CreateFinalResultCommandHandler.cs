using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Application.Features.FinalResults.Commands.CreateFinalResult.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.FinalResults.Commands.CreateFinalResult
{
    public class CreateFinalResultCommandHandler : IRequestHandler<CreateFinalResultCommand, Result<CreateFinalResultResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateFinalResultCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<CreateFinalResultResponseModel>> Handle(CreateFinalResultCommand request, CancellationToken cancellationToken)
        {
            var m = request.Model;

            // 1. Phải nhập ĐÚNG MỘT phạm vi: RoundId | EventId | TrackId
            int scope = (!string.IsNullOrEmpty(m.RoundId) ? 1 : 0)
                      + (!string.IsNullOrEmpty(m.EventId) ? 1 : 0)
                      + (!string.IsNullOrEmpty(m.TrackId) ? 1 : 0);
            if (scope != 1)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Phải nhập đúng MỘT trong: RoundId (kết quả vòng), EventId (kết quả toàn sự kiện), hoặc TrackId (kết quả 1 hạng mục).");
            }

            // 2. Thực thể phạm vi phải tồn tại
            if (!string.IsNullOrEmpty(m.RoundId) &&
                !await _unitOfWork.GetRepository<Round>().AnyAsync(r => r.Id == m.RoundId, cancellationToken))
                return BaseException.BadRequestNotFoundResponse($"Vòng thi '{m.RoundId}' không tồn tại.");

            if (!string.IsNullOrEmpty(m.EventId) &&
                !await _unitOfWork.GetRepository<Event>().AnyAsync(e => e.Id == m.EventId, cancellationToken))
                return BaseException.BadRequestNotFoundResponse($"Sự kiện '{m.EventId}' không tồn tại.");

            if (!string.IsNullOrEmpty(m.TrackId) &&
                !await _unitOfWork.GetRepository<Track>().AnyAsync(t => t.Id == m.TrackId, cancellationToken))
                return BaseException.BadRequestNotFoundResponse($"Hạng mục '{m.TrackId}' không tồn tại.");

            // 3. Upsert: tìm theo Team + đúng phạm vi -> có thì cập nhật, chưa có thì tạo
            var repo = _unitOfWork.GetRepository<FinalResult>();
            var finalResult = await repo.Entities.FirstOrDefaultAsync(
                f => f.TeamId == m.TeamId
                  && f.RoundId == m.RoundId
                  && f.EventId == m.EventId
                  && f.TrackId == m.TrackId,
                cancellationToken);

            bool isNew = finalResult == null;
            if (isNew)
            {
                finalResult = new FinalResult
                {
                    TeamId = m.TeamId,
                    RoundId = m.RoundId,
                    EventId = m.EventId,
                    TrackId = m.TrackId
                };
            }

            finalResult!.FinalScore = m.FinalScore;
            finalResult.Rank = m.Rank;
            finalResult.IsAdvanced = m.IsAdvanced;

            if (isNew)
                await repo.AddAsync(finalResult);
            else
                await repo.UpdateAsync(finalResult);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateFinalResultResponseModel
            {
                Id = finalResult.Id,
                TeamId = finalResult.TeamId,
                RoundId = finalResult.RoundId,
                EventId = finalResult.EventId,
                TrackId = finalResult.TrackId,
                FinalScore = finalResult.FinalScore,
                Rank = finalResult.Rank,
                IsAdvanced = finalResult.IsAdvanced,
                CreatedTime = finalResult.CreatedTime
            };
        }
    }
}

