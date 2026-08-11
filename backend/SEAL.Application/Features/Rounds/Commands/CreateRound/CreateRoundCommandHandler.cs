using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Rounds.Commands.CreateRound.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Rounds.Commands.CreateRound
{
    public class CreateRoundCommandHandler : IRequestHandler<CreateRoundCommand, Result<CreateRoundResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateRoundCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<CreateRoundResponseModel>> Handle(CreateRoundCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra sự tồn tại của Event và lấy thông tin thời gian
            var parentEvent = await _unitOfWork.GetRepository<Event>().GetByIdAsync(request.Model.EventId);
            if (parentEvent == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Sự kiện có ID '{request.Model.EventId}' không tồn tại.");
            }

            var startDateUtc = request.Model.StartDate.ToUniversalTime();
            var endDateUtc = request.Model.EndDate.ToUniversalTime();

            // Thời gian bắt đầu phải TRƯỚC thời gian kết thúc (vòng "âm" làm sụp mọi logic
            // cửa sổ nộp bài/chấm điểm dựa trên StartDate/EndDate).
            if (startDateUtc >= endDateUtc)
            {
                return BaseException.BadRequestResponse("Thời gian bắt đầu vòng thi phải trước thời gian kết thúc.");
            }

            // Ràng buộc thời gian với Event
            if (startDateUtc < parentEvent.StartDate || endDateUtc > parentEvent.EndDate)
            {
                return BaseException.BadRequestResponse($"Thời gian diễn ra vòng thi phải nằm trong khoảng thời gian của sự kiện ({parentEvent.StartDate:dd/MM/yyyy HH:mm} - {parentEvent.EndDate:dd/MM/yyyy HH:mm}).");
            }

            // Ràng buộc cửa sổ chấm điểm đối chiếu với Sự kiện (Event)
            if (request.Model.ScoringStartDate.HasValue && request.Model.ScoringStartDate.Value.ToUniversalTime() > parentEvent.EndDate)
            {
                return BaseException.BadRequestResponse("Thời gian bắt đầu chấm không được vượt quá thời gian kết thúc sự kiện.");
            }
            if (request.Model.ScoringEndDate.HasValue && request.Model.ScoringEndDate.Value.ToUniversalTime() > parentEvent.EndDate)
            {
                return BaseException.BadRequestResponse("Hạn chấm không được vượt quá thời gian kết thúc sự kiện.");
            }

            // 2. Kiểm tra trùng lặp RoundName trong cùng Event
            var isDuplicateName = await _unitOfWork.GetRepository<Round>().AnyAsync(
                r => r.RoundName.ToLower() == request.Model.RoundName.ToLower() && r.EventId == request.Model.EventId,
                cancellationToken);

            if (isDuplicateName)
            {
                return BaseException.BadRequestDupplicationResponse($"Vòng thi '{request.Model.RoundName}' đã tồn tại trong sự kiện này.");
            }

            // Kiểm tra trùng lặp RoundNumber trong cùng Event
            var isDuplicateNumber = await _unitOfWork.GetRepository<Round>().AnyAsync(
                r => r.RoundNumber == request.Model.RoundNumber && r.EventId == request.Model.EventId,
                cancellationToken);

            if (isDuplicateNumber)
            {
                return BaseException.BadRequestDupplicationResponse($"Số thứ tự vòng thi '{request.Model.RoundNumber}' đã tồn tại trong sự kiện này.");
            }

            // 3. Tạo mới thực thể Round
            var round = new Round
            {
                EventId = request.Model.EventId,
                RoundName = request.Model.RoundName,
                RoundNumber = request.Model.RoundNumber,
                StartDate = request.Model.StartDate.ToUniversalTime(),
                EndDate = request.Model.EndDate.ToUniversalTime(),
                AdvancementRule = request.Model.AdvancementRule,
                ScoringStartDate = request.Model.ScoringStartDate?.ToUniversalTime(),
                ScoringEndDate = request.Model.ScoringEndDate?.ToUniversalTime()
            };

            await _unitOfWork.GetRepository<Round>().AddAsync(round);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateRoundResponseModel
            {
                Id = round.Id,
                EventId = round.EventId,
                RoundName = round.RoundName,
                RoundNumber = round.RoundNumber,
                StartDate = round.StartDate,
                EndDate = round.EndDate,
                AdvancementRule = round.AdvancementRule,
                ScoringStartDate = round.ScoringStartDate,
                ScoringEndDate = round.ScoringEndDate,
                CreatedTime = round.CreatedTime
            };
        }
    }
}

