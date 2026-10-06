using System.Text.Json.Serialization;

namespace TonKhoApp.Application.Common
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string[]>? Errors { get; set; }

        public static ApiResponse Ok(string? message = null, object? data = null)
        {
            return new ApiResponse { Success = true, Message = message, Data = data };
        }

        public static ApiResponse Fail(string message, Dictionary<string, string[]>? errors = null)
        {
            return new ApiResponse { Success = false, Message = message, Errors = errors };
        }
    }
}
