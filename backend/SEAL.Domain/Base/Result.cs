using System.Text.Json.Serialization;

namespace SEAL_Domain.Base
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BaseException.ErrorDetail? Error { get; }
        
        public int StatusCode { get; }

        protected Result(bool isSuccess, BaseException.ErrorDetail? error, int statusCode)
        {
            if (isSuccess && error != null)
                throw new InvalidOperationException();
            if (!isSuccess && error == null)
                throw new InvalidOperationException();

            IsSuccess = isSuccess;
            Error = error;
            StatusCode = statusCode;
        }

        public static Result Success() => new Result(true, null, 200);

        public static Result Failure(BaseException.ErrorException exception)
        {
            return new Result(false, exception.ErrorDetail, exception.StatusCode);
        }

        public static Result<T> Success<T>(T value) => new Result<T>(value, true, null, 200);
        
        public static Result<T> Failure<T>(BaseException.ErrorException exception)
        {
            return new Result<T>(default, false, exception.ErrorDetail, exception.StatusCode);
        }
        public static implicit operator Result(BaseException.ErrorException exception) => Failure(exception);
        
        public static implicit operator Result(BaseException.UnauthorizedException exception) 
            => new Result(false, new BaseException.ErrorDetail { ErrorMessage = exception.Message, ErrorCode = "unauthorized" }, 401);
            
        public static implicit operator Result(BaseException.ForbiddenException exception) 
            => new Result(false, new BaseException.ErrorDetail { ErrorMessage = exception.Message, ErrorCode = "forbidden" }, 403);
    }

    public class Result<T> : Result
    {
        private readonly T? _value;

        public T Value => IsSuccess 
            ? _value! 
            : throw new InvalidOperationException("Cannot access the value of a failure result.");

        protected internal Result(T? value, bool isSuccess, BaseException.ErrorDetail? error, int statusCode)
            : base(isSuccess, error, statusCode)
        {
            _value = value;
        }
        
        public static implicit operator Result<T>(T value) => Success(value);
        public static implicit operator Result<T>(BaseException.ErrorException exception) => Failure<T>(exception);
        
        public static implicit operator Result<T>(BaseException.UnauthorizedException exception) 
            => new Result<T>(default, false, new BaseException.ErrorDetail { ErrorMessage = exception.Message, ErrorCode = "unauthorized" }, 401);
            
        public static implicit operator Result<T>(BaseException.ForbiddenException exception) 
            => new Result<T>(default, false, new BaseException.ErrorDetail { ErrorMessage = exception.Message, ErrorCode = "forbidden" }, 403);
    }
}
