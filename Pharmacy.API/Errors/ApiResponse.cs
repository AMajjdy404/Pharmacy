
namespace Pharmacy.API.Errors
{
    public class ApiResponse
    {
        private int v;
        private IEnumerable<string> enumerable;

        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public ApiResponse(int statusCode, string? message = null)
        {
            StatusCode = statusCode;
            Message = message ?? GetDefaultMessageForStatusCode(StatusCode);
        }

        public ApiResponse(int v, IEnumerable<string> enumerable)
        {
            this.v = v;
            this.enumerable = enumerable;
        }

        private string? GetDefaultMessageForStatusCode(int? statusCode)
        {
            return statusCode switch
            {
                200 => "OK",
                400 => "Bad Request",
                401 => "You Are Not Authorized",
                404 => "Resource Not Found",
                500 => "Internal Server Error",
                _ => null

            };
        }
    }
}
