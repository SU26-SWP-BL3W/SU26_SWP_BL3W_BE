using MediatR;
using SEAL_Application.Features.Prizes.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Prizes.Commands.UpdatePrize
{
    public class UpdatePrizeCommandHandler : IRequestHandler<UpdatePrizeCommand, Result<PrizeModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public UpdatePrizeCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<PrizeModel>> Handle(UpdatePrizeCommand request, CancellationToken cancellationToken)
        {
            var prize = await _unitOfWork.GetRepository<Prize>().GetByIdAsync(request.PrizeId);
            if (prize == null) return BaseException.BadRequestNotFoundResponse("Prize not found");

            var accessDenied = await PrizeAccessHelper.EnsureCanManageEventPrizesAsync(
                _unitOfWork, _currentUserService, _eventRoleChecker, prize.EventId, cancellationToken);
            if (accessDenied != null) return accessDenied;

            if (!string.IsNullOrEmpty(request.Payload.TrackId))
            {
                var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(request.Payload.TrackId);
                if (track == null || track.EventId != prize.EventId)
                {
                    return BaseException.BadRequestNotFoundResponse("Track không tồn tại hoặc không thuộc sự kiện này.");
                }
            }

            if (request.Payload.Quantity < prize.Quantity)
            {
                var currentAssignedCount = _unitOfWork.GetRepository<FinalResult>()
                    .Entities.Count(x => x.PrizeId == prize.Id);
                if (request.Payload.Quantity < currentAssignedCount)
                {
                    return BaseException.BadRequestResponse($"Không thể hạ số lượng giải xuống dưới số đã gán hiện tại ({currentAssignedCount}).");
                }
            }

            prize.TrackId = request.Payload.TrackId;
            prize.PrizeName = request.Payload.PrizeName;
            prize.Value = request.Payload.Value;
            prize.Quantity = request.Payload.Quantity;

            await _unitOfWork.GetRepository<Prize>().UpdateAsync(prize);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var model = new PrizeModel
            {
                Id = prize.Id,
                EventId = prize.EventId,
                TrackId = prize.TrackId,
                PrizeName = prize.PrizeName,
                Value = prize.Value,
                Quantity = prize.Quantity
            };
            return model;
        }
    }
}
