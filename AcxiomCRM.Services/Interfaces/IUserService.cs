using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Services.Interfaces
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public interface IUserService
    {
        Task<List<UserDto>> GetUsersAsync(string? search);
        Task<UserDto?> GetUserByIdAsync(string id);
        Task<(bool Success, string Message, string? UserId)> CreateUserAsync(string fullName, string email, string password, string role, string currentUserId, string ipAddress);
        Task<(bool Success, string Message)> UpdateUserAsync(string id, string fullName, string email, string role, bool isActive, string currentUserId, string ipAddress);
        Task<(bool Success, string Message)> ToggleUserStatusAsync(string id, bool isActive, string currentUserId, string ipAddress);
        Task<(bool Success, string Message)> ResetPasswordAsync(string id, string newPassword, string currentUserId, string ipAddress);
        Task<(bool Success, string Message)> UnlockUserAsync(string id, string currentUserId, string ipAddress);
        Task<List<ApplicationRole>> GetRolesAsync();
    }
}
