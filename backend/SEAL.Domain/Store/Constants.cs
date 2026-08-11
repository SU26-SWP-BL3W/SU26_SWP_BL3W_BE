namespace SEAL_Domain.Store
{
    public static class Constants
    {
        public class ResponseCodeConstants
        {
            public const string NOT_FOUND = "Not found!";
            public const string SUCCESS = "Success!";
            public const string FAILED = "Failed!";
            public const string EXISTED = "Existed!";
            public const string DUPLICATE = "Duplicate!";
            public const string INTERNAL_SERVER_ERROR = "INTERNAL_SERVER_ERROR";
            public const string INVALID_INPUT = "Invalid input!";
            public const string UNAUTHORIZED = "Unauthorized!";
            public const string FORBIDDEN = "Forbidden!";
            public const string BADREQUEST = "Bad request!";
        }

        public class MessagesConstants
        {
            public const string DELETE_SUCCESS = "Xóa thành công";
            public const string UPDATE_SUCCESS = "Cập nhật thành công";
            public const string CREATE_SUCCESS = "Tạo mới thành công";
            public const string SUCCESS = "Thành công";

        }

        public static class ValidationMessages
        {
            public const string InvalidCode = "Mã quy trình không được dưới 3 kí tự và không chứa kí tự đặc biệt.";
            public const string InvalidAccount = "Không thể xác thực tài khoản.";
            public const string InvalidValidation = "Có lỗi khi valid data";
            public const string InvalidName = "Tên quy trình không được để trống và không quá 50 ký tự.";
            public const string InvalidUserName = "Tên đăng nhập không được để trống và không được dưới 5 kí tự.";
            public const string InvalidPassword = "Mật khẩu không được để trống và có ít nhất 8 kí tự.";
            public const string InvalidConfirm = "Mật khẩu mới và xác nhận mật khẩu không khớp.";
            public const string InvalidNewPassword = "Xác nhận mật khẩu không được để trống.";
            public const string InvalidNotFound = "Không tìm thấy: ";
            public const string InvalidNotFoundUserName = InvalidNotFound + "Username";
            public const string InvalidNotFoundAccount = InvalidNotFound + "tài khoản";
            public const string InvalidType = "Định dạng không được để trống";
            public const string InvalidEstimatedTime = "Thời gian ước tính không được nhỏ hơn không";

            public const string InvalidEmail = "Email không được để trống và phải đúng định dạng vd: user@example.com";
            public const string InvalidPhone = "Số điện thoại không được để trống và chỉ được chứa số";
            public const string InvalidRoleIds = "Danh sách role không được trống";
            public const string InvalidFullName = "Họ và tên không được để trống.";
            public const string InvalidProviderId = "ProviderId không được để trống khi sử dụng social login.";

            //exist
            public const string ExistEmail = "Email đã được sử dụng.";
            public const string ExistPhone = "Số điện thoại đã được sử dụng.";
            public const string ExistUsername = "Username đã được sử dụng.";
            public const string ExistInfor = "Số điện thoại hoặc email đã được sử dụng.";

            //api response
            public const string InvalidUserRoleResponse = "Có lỗi xảy ra ở user role";
            public const string InvalidDepartmentUserResponse = "Có lỗi xảy ra ở department user";

            public const string EmailNotRegistered = "Email chưa được đăng ký trong hệ thống.";
            public const string InvalidOtp = "Mã OTP không chính xác.";
            public const string OtpExpired = "Mã OTP đã hết hạn.";
            public const string AccountNotActive = "Tài khoản chưa được kích hoạt. Vui lòng xác thực email của bạn.";
        }
    }
}
