namespace TrungTinElectronicsAPI.Models.DTOs
{
    public class ApiResponse<T>
    {
        public string Message { get; set; }
        public int Result { get; set; }
        public T? Data { get; set; }

        public ApiResponse(string message, int result, T? data = default)
        {
            Message = message;
            Result = result;
            Data = data;
        }
    }
}
