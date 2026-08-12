using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.Events.Commands.UpdateEvent.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SEAL_Application.Features.Events.Commands.UpdateEvent
{
    public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, Result<UpdateEventResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateEventCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UpdateEventResponseModel>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm sự kiện
            var eventEntity = await _unitOfWork.GetRepository<Event>().GetByIdAsync(request.Id);
            if (eventEntity == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Sự kiện có ID '{request.Id}' không tồn tại.");
            }

            // 2. Kiểm tra trùng tên sự kiện trong cùng 1 năm (nếu có thay đổi tên hoặc năm)
            var isDuplicate = await _unitOfWork.GetRepository<Event>().AnyAsync(
                e => e.EventName.ToLower() == request.Model.EventName.ToLower() && e.Year == request.Model.Year && e.Id != request.Id,
                cancellationToken);

            if (isDuplicate)
            {
                return BaseException.BadRequestDupplicationResponse($"Sự kiện '{request.Model.EventName}' trong năm {request.Model.Year} đã tồn tại.");
            }

            // 2b. THU HẸP thời gian sự kiện không được làm vòng thi hiện có LỌT RA NGOÀI
            //     (bất biến "vòng nằm trong sự kiện" chỉ được kiểm lúc tạo/sửa vòng — sửa event thì không).
            var newStartUtc = request.Model.StartDate.ToUniversalTime();
            var newEndUtc = request.Model.EndDate.ToUniversalTime();
            var hasRoundOutside = await _unitOfWork.GetRepository<Round>().AnyAsync(
                r => r.EventId == eventEntity.Id && (r.StartDate < newStartUtc || r.EndDate > newEndUtc),
                cancellationToken);
            if (hasRoundOutside)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Khoảng thời gian mới làm ít nhất một vòng thi lọt ra ngoài sự kiện. Vui lòng điều chỉnh thời gian các vòng thi trước.");
            }

            // 3. Cập nhật thông tin
            eventEntity.EventName = request.Model.EventName;
            eventEntity.Season = request.Model.Season;
            eventEntity.Year = request.Model.Year;
            eventEntity.StartDate = request.Model.StartDate.ToUniversalTime();
            eventEntity.EndDate = request.Model.EndDate.ToUniversalTime();

            // 2c. Kiểm tra MaxTeams không được nhỏ hơn số đội hiện tại (đang active)
            if (request.Model.MaxTeams < eventEntity.MaxTeams)
            {
                var currentTeamCount = await _unitOfWork.GetRepository<Team>().GetQueryable()
                    .CountAsync(t => t.EventId == eventEntity.Id && t.IsActive, cancellationToken);
                
                if (request.Model.MaxTeams < currentTeamCount)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        $"Số đội tối đa ({request.Model.MaxTeams}) không được nhỏ hơn số đội hiện tại đã đăng ký ({currentTeamCount} đội).");
                }
            }

            eventEntity.RegistrationStartDate = request.Model.RegistrationStartDate?.ToUniversalTime();
            eventEntity.RegistrationEndDate = request.Model.RegistrationEndDate?.ToUniversalTime();
            eventEntity.Description = request.Model.Description;
            eventEntity.Status = request.Model.Status;
            eventEntity.PhotoEventUrl = request.Model.PhotoEventUrl;
            eventEntity.MaxTeams = request.Model.MaxTeams;
            eventEntity.LastUpdatedTime = CoreHelper.SystemTimeNow;

            // 4. Lưu vào Database
            await _unitOfWork.GetRepository<Event>().UpdateAsync(eventEntity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateEventResponseModel
            {
                Id = eventEntity.Id,
                EventName = eventEntity.EventName,
                Season = eventEntity.Season,
                Year = eventEntity.Year,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate,
                RegistrationStartDate = eventEntity.RegistrationStartDate,
                RegistrationEndDate = eventEntity.RegistrationEndDate,
                Description = eventEntity.Description,
                Status = eventEntity.Status,
                PhotoEventUrl = eventEntity.PhotoEventUrl,
                LastUpdatedTime = eventEntity.LastUpdatedTime
            };
        }
    }
}

