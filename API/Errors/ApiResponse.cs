namespace API.Errors
{
    public class ApiResponse
    {
        public ApiResponse(int statusCode, string message = null)
        {
            StatusCode = statusCode;
            Message = message ?? GetDefaultMessageForStatusCode(statusCode);
        }

       

        public int StatusCode { get; set; }
        public string Message { get; set; }
        
         private string GetDefaultMessageForStatusCode(int statusCode)
        {
            return statusCode switch
            {
                400 => "You've made a bad request",
                401 => "You're not authorised",
                404 => "Resource not found",
                500 => "500 error",
                _ => null
            };
        }
    }
}