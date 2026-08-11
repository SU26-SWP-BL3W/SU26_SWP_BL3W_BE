using MediatR;
using SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultById.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultById
{
    public class GetSubmitResultByIdQueryHandler : IRequestHandler<GetSubmitResultByIdQuery, Result<SubmitResultDetailResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSubmitResultByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<SubmitResultDetailResponseModel>> Handle(GetSubmitResultByIdQuery request, CancellationToken cancellationToken)
        {
            // Tìm ki?m th?ng b?ng string
            var sr = await _unitOfWork.GetRepository<SubmitResult>().GetByIdAsync(request.Id);
            if (sr == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Bài n?p không t?n t?i.");
            }

            // Truy v?n quan h? tr?c ti?p b?ng string
            var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(sr.TeamId);
            var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(sr.TrackId);

            return new SubmitResultDetailResponseModel
            {
                Id = sr.Id,
                TeamId = sr.TeamId,
                TeamName = team?.Name ?? "Không xác d?nh",
                TrackId = sr.TrackId,
                TrackName = track?.TrackName ?? "Không xác d?nh",
                SubmissionUrl = sr.SubmissionUrl,
                Description = sr.Description,
                IsActive = sr.IsActive,
                CreatedTime = sr.CreatedTime
            };
        }
    }
}
