namespace ZainEMPProtal.Models
{
    public class ServiceResponse<T>
    {
        public bool IsSuccess { get; set; }  // Indicates if the operation was successful
        public T Data { get; set; }         // Holds the actual data of the response
        public string ErrorMessage { get; set; }  // Error message in case of failure
        public string ErrorMessageAr { get; set; }  // Error message in case of failure
        public int StatusCode { get; set; }   // HTTP status code or custom code

        public ServiceResponse()
        {
            // Default constructor
        }

        public ServiceResponse(bool isSuccess, T data, int statusCode, string errorMessage = "Operation Done Successfully", string errorMessageAr = "تمت العملية بنجاح")
        {
            IsSuccess = isSuccess;
            Data = data;
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
            ErrorMessageAr = errorMessageAr;
        }
    }

}
