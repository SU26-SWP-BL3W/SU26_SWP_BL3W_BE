using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SEAL_Application.Commons;
using SEAL_Application.Features.Users.Commands.CreateUser.Models;
using SEAL_Application.Features.Users.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.UpdateStudentProfile
{
    public class UpdateStudentProfileCommandHandler : IRequestHandler<UpdateStudentProfileCommand, Result<UserModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string? _fptMockBaseUrl;
        private readonly string? _fptMockApiKey;

        public UpdateStudentProfileCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _httpClientFactory = httpClientFactory;
            _fptMockBaseUrl = configuration["FptMockApi:BaseUrl"];
            _fptMockApiKey = configuration["FptMockApi:ApiKey"];
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

            // 2. Nếu là sinh viên FPT, xác thực qua FPT Mock API
            if (request.IsFpt)
            {
                if (string.IsNullOrEmpty(request.StudentCode))
                {
                    return BaseException.BadRequestResponse("Mã sinh viên là bắt buộc đối với sinh viên FPT University.");
                }

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Clear();
                // Chỉ thêm header khi có API key (tránh ArgumentException khi key null)
                if (!string.IsNullOrEmpty(_fptMockApiKey))
                {
                    httpClient.DefaultRequestHeaders.Add("X-API-KEY", _fptMockApiKey);
                }

                FptStudentResponse? fptResponse;
                try
                {
                    fptResponse = await httpClient.GetFromJsonAsync<FptStudentResponse>(
                        $"{_fptMockBaseUrl}/api/fpt-mock/students/{request.StudentCode}",
                        cancellationToken
                    );
                }
                catch (HttpRequestException)
                {
                    // Hệ thống xác thực SV FPT không gọi được (chưa bật / sập / sai URL)
                    // -> trả lỗi rõ ràng thay vì 500 "An unexpected error occurred".
                    return BaseException.BadRequestResponse(
                        "Chưa kết nối được hệ thống xác thực sinh viên FPT. Vui lòng thử lại sau hoặc liên hệ ban tổ chức.");
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Timeout khi gọi API FPT (không phải do client tự hủy request)
                    return BaseException.BadRequestResponse(
                        "Hệ thống xác thực sinh viên FPT phản hồi quá lâu. Vui lòng thử lại sau.");
                }

                if (fptResponse == null || !fptResponse.IsValid)
                {
                    return BaseException.BadRequestResponse(
                        fptResponse?.Message ?? "Mã sinh viên không hợp lệ hoặc không thuộc FPT University."
                    );
                }

                var apiEmail = fptResponse.Data?.Email;
                var apiFullName = fptResponse.Data?.FullName;

                if (NormalizeString(user.Email) != NormalizeString(apiEmail))
                {
                    return BaseException.BadRequestResponse("Email tài khoản không khớp với thông tin sinh viên tại FPT University.");
                }

                fullName = apiFullName ?? fullName;
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


