using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IActivityService
    {
        Task<List<Activity>> GetActivitiesAsync(string currentUserId, string currentUserRole, ActivityType? searchType, DateTime? searchDate, ActivityStatus? searchStatus, string? searchAssignedUser);
        Task<Activity?> GetActivityByIdAsync(int id, string currentUserId, string currentUserRole);
        Task<(bool Success, string Message, Activity? Activity)> CreateActivityAsync(Activity activity, string currentUserId, string currentUserRole, string ipAddress);
        Task<(bool Success, string Message)> UpdateActivityAsync(Activity activity, string currentUserId, string currentUserRole, string ipAddress);
        Task<(bool Success, string Message)> DeleteActivityAsync(int id, string currentUserId, string currentUserRole, string ipAddress);
    }
}
