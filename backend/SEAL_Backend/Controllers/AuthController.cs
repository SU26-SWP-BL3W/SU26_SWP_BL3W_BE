using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.Users.Commands.LoginUser;
using SEAL_Application.Features.Users.Commands.LoginUser.Models;
using SEAL_Application.Features.Users.Commands.GoogleLogin;
using SEAL_Application.Features.Users.Commands.RegisterUser;
using SEAL_Application.Features.Users.Commands.RegisterUser.Models;
using SEAL_Application.Features.Users.Commands.RefreshToken;
using SEAL_Application.Features.Users.Commands.RefreshToken.Models;
using SEAL_Application.Features.Users.Commands.RequestUnblock;
using SEAL_Application.Features.Users.Commands.RequestUnblock.Models;
using SEAL_Application.Features.Users.Commands.ForgotPassword;
using SEAL_Application.Features.Users.Commands.ResetPassword;
using SEAL_Application.Features.Users.Commands.Logout;
using SEAL_Application.Features.Users.Commands.ChangePassword;
using SEAL_Application.Features.Users.Models;
using SEAL_Backend.Helpers;
using SEAL_Domain.Base;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<UserModel>), 200)]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequestModel requestModel)
        {
            var result = await _mediator.Send(new RegisterUserCommand { Model = requestModel });
            return OkResponse(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<LoginUserResponseModel>), 200)]
        public async Task<IActionResult> Login([FromBody] LoginUserRequestModel requestModel)
        {
            var result = await _mediator.Send(new LoginUserCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Đăng nhập bằng Google (Sign in with Google). FE gửi idToken lấy từ Google Identity Services.
        /// Lần đầu sẽ tự tạo tài khoản theo email Google.
        /// </summary>
        [HttpPost("google-login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<LoginUserResponseModel>), 200)]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestModel requestModel)
        {
            var result = await _mediator.Send(new GoogleLoginCommand { Model = requestModel });
            return OkResponse(result);
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<RefreshTokenResponseModel>), 200)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestModel requestModel)
        {
            var result = await _mediator.Send(new RefreshTokenCommand(requestModel));
            return OkResponse(result);
        }

        [HttpPost("request-unblock")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<RequestUnblockResponseModel>), 200)]
        public async Task<IActionResult> RequestUnblock([FromBody] RequestUnblockRequestModel requestModel)
        {
            var result = await _mediator.Send(new RequestUnblockCommand { Model = requestModel });
            return OkResponse(result);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<ForgotPasswordResponseModel>), 200)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestModel requestModel)
        {
            var result = await _mediator.Send(new ForgotPasswordCommand { Model = requestModel });
            return OkResponse(result);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestModel requestModel)
        {
            var result = await _mediator.Send(new ResetPasswordCommand { Model = requestModel });
            return OkResponse(result);
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Logout()
        {
            var userId = SEAL_Infrastructure.Services.Token.GetUserIdFromHttpContext(HttpContext);
            var result = await _mediator.Send(new LogoutCommand(userId));
            return OkResponse(result);
        }

        [HttpPut("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestModel requestModel)
        {
            var userId = SEAL_Infrastructure.Services.Token.GetUserIdFromHttpContext(HttpContext);
            var result = await _mediator.Send(new ChangePasswordCommand(userId, requestModel));
            return OkResponse(result);
        }

        [HttpGet("verify-email")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var result = await _mediator.Send(new SEAL_Application.Features.Users.Commands.VerifyEmail.VerifyEmailCommand { Token = token });
            return OkResponse(result);
        }

        [HttpPost("student-profiles")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<UserModel>), 200)]
        public async Task<IActionResult> CreateStudentProfile([FromBody] SEAL_Application.Features.Users.Commands.UpdateStudentProfile.UpdateStudentProfileCommand command)
        {
            var result = await _mediator.Send(command);
            return OkResponse(result);
        }

        [HttpPut("student-profiles")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<UserModel>), 200)]
        public async Task<IActionResult> UpdateStudentProfile([FromBody] SEAL_Application.Features.Users.Commands.UpdateStudentProfile.UpdateStudentProfileCommand command)
        {
            var result = await _mediator.Send(command);
            return OkResponse(result);
        }
    }
}
