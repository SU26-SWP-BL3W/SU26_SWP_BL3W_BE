
using SEAL_Domain.Ultis;

namespace SEAL_Domain.Store
{
    public enum StatusCodeHelper
    {
        [CustomName("Success")]
        OK = 200,

        [CustomName("Bad Request")]
        BadRequest = 400,

        [CustomName("Unauthorized")]
        Unauthorized = 401,

        [CustomName("Internal Server Error")]
        ServerError = 500,
        [CustomName("Forbidden")]
        Forbidden = 403,

        [CustomName("Not Found")]
        NotFound = 404,
    }
}
