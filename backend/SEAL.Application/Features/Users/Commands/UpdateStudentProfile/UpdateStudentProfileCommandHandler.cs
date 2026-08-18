using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Commons;
using SEAL_Application.Features.Users.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.UpdateStudentProfile
{
    public class UpdateStudentProfileCommandHandler : IRequestHandler<UpdateStudentProfileCommand, Result<UserModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public UpdateStudentProfileCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<UserModel>> Handle(UpdateStudentProfileCommand request, CancellationToken cancellationToken)
        {
            // 1. Lấy người dùng hiện tại
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return BaseException.BadRequestNotFoundResponse("Tài khoản người dùng không tồn tại.");
            }

            // 1a. Chặn tài khoản quản trị nộp hồ sơ thí sinh.
            //     Handler này ghi đè chính user đang đăng nhập (FullName, IsStudent...) nên nếu admin
            //     lỡ bấm gửi form hồ sơ thì tài khoản admin sẽ bị ghi đè (đổi tên, gắn IsStudent).
            //     Chặn tại BE = phòng thủ chắc chắn kể cả khi FE lỡ hiển thị form cho admin.
            if (user.IsAdmin)
            {
                return BaseException.BadRequestResponse("Tài khoản quản trị không thể nộp hồ sơ thí sinh.");
            }

            // 1b. Business Rule: nếu đã bị từ chối >= 2 lần thì không cho cập nhật hồ sơ nữa.
            //     Đếm TỔNG số bản ghi UserRejection (không lọc IsActive) để không bị reset khi nộp lại.
            var rejectionCount = await _unitOfWork.GetRepository<UserRejection>().Entities
                .CountAsync(r => r.UserId == userId, cancellationToken);
            if (rejectionCount >= 2)
            {
                return new BaseException.ForbiddenException(
                    "Hồ sơ của bạn đã bị từ chối 2 lần nên không thể cập nhật nữa. Vui lòng liên hệ ban tổ chức.");
            }

            string fullName = user.FullName;

            // 2. Nếu là sinh viên FPT, xác thực qua bảng FptStudents thật trong DB
            //    (trước đây gọi ra "https://localhost:7087/api/Students/..." — địa chỉ chưa
            //    từng tồn tại, luôn fail, khiến sinh viên FPT KHÔNG BAO GIỜ nộp hồ sơ thành
            //    công được. Nay đọc thẳng cùng bảng mà endpoint xác thực preview đang dùng.)
            if (request.IsFpt)
            {
                if (string.IsNullOrEmpty(request.StudentCode))
                {
                    return BaseException.BadRequestResponse("Mã sinh viên là bắt buộc đối với sinh viên FPT University.");
                }

                var fptStudent = await _unitOfWork.GetRepository<FptStudent>().Entities
                    .FirstOrDefaultAsync(s => s.StudentCode.ToLower() == request.StudentCode.ToLower(), cancellationToken);

                if (fptStudent == null || !string.Equals(fptStudent.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                {
                    return BaseException.BadRequestResponse("Mã sinh viên không hợp lệ hoặc không thuộc FPT University.");
                }

                if (NormalizeString(user.Email) != NormalizeString(fptStudent.Email))
                {
                    return BaseException.BadRequestResponse("Email tài khoản không khớp với thông tin sinh viên tại FPT University.");
                }

                fullName = fptStudent.FullName;
            }
            else
            {
                // Nếu ngoài FPT thì ảnh thẻ sinh viên là bắt buộc
                if (string.IsNullOrEmpty(request.PhotoStudentCardUrl))
                {
                    return BaseException.BadRequestResponse("Ảnh thẻ sinh viên là bắt buộc đối với sinh viên ngoài FPT University.");
                }

                if (!string.IsNullOrEmpty(request.FullName))
                {
                    fullName = request.FullName;
                }
            }

            // 3. Cập nhật hồ sơ sinh viên
            user.SchoolId = request.SchoolId;
            user.StudentCode = request.StudentCode;
            user.PhotoStudentCardUrl = request.IsFpt ? null : request.PhotoStudentCardUrl;
            user.IsFpt = request.IsFpt;
            user.IsStudent = true;
            user.FullName = fullName;
            user.IsApproved = request.IsFpt; // Tự động duyệt nếu là sinh viên FPT, ngược lại false chờ duyệt thủ công
            user.IsTemporary = false; // Tài khoản tạm (được mời vào đội) trở thành tài khoản sinh viên thật sau khi hoàn tất hồ sơ

            await _unitOfWork.GetRepository<User>().UpdateAsync(user);

            // 4. Vô hiệu hóa lịch sử từ chối cũ
            var rejections = await _unitOfWork.GetRepository<UserRejection>().Entities
                .Where(r => r.UserId == user.Id && r.IsActive)
                .ToListAsync(cancellationToken);

            if (rejections.Any())
            {
                foreach (var rejection in rejections)
                {
                    rejection.IsActive = false;
                }
                await _unitOfWork.GetRepository<UserRejection>().UpdateRangeAsync(rejections);
            }

            // 5. Nếu được tự động duyệt, kiểm tra và duyệt đội thi liên quan
            // (ĐÃ BỎ) Trước đây nộp/duyệt hồ sơ sẽ TỰ ĐỘNG đưa đội Forming -> Registered.
            // Nay xét duyệt ở cấp ĐỘI THI: đội tự chốt danh sách (-> PendingApproval) rồi
            // EC/Admin duyệt qua ApproveTeamRegistration.

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UserModel
            {
                Id = user.Id,
                SchoolId = user.SchoolId ?? string.Empty,
                StudentCode = user.StudentCode,
                Email = user.Email,
                FullName = user.FullName,
                IsStudent = user.IsStudent,
                IsAdmin = user.IsAdmin,
                IsApproved = user.IsApproved,
                IsFpt = user.IsFpt,
                PhotoStudentCardUrl = user.PhotoStudentCardUrl
            };
        }

        private static string NormalizeString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Replace(" ", "").ToLower();
        }
    }
}


