using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Prizes.Commands.DeletePrize
{
    public class DeletePrizeCommandHandler : IRequestHandler<DeletePrizeCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeletePrizeCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(DeletePrizeCommand request, CancellationToken cancellationToken)
        {
            var prize = await _unitOfWork.GetRepository<Prize>().GetByIdAsync(request.PrizeId);
            if (prize == null) return BaseException.BadRequestNotFoundResponse("Prize not found");

            var hasAssignedResults = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                fr => fr.PrizeId == prize.Id, cancellationToken);
            if (hasAssignedResults)
            {
                return BaseException.BadRequestInvaildInputResponse("Giải thưởng đang được gán cho kết quả nên không thể xóa.");
            }

            await _unitOfWork.GetRepository<Prize>().DeleteAsync(prize);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
