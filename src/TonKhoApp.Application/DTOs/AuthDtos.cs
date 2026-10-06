using System.ComponentModel.DataAnnotations;

namespace TonKhoApp.Application.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "User ID không được để trống")]
        [MinLength(2, ErrorMessage = "User ID phải có ít nhất 2 ký tự")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password không được để trống")]
        [MinLength(6, ErrorMessage = "Password phải có ít nhất 6 ký tự")]
        public string UserPassword { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiration { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiration { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    }

    public class RefreshTokenRequest
    {
        public string? RefreshToken { get; set; }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public LoginResponse? Data { get; set; }
    }
    // JSON: { userId, fullName, roles }
    public class UserListItemDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    }
}
