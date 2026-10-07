using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IFollowUpService
    {
        Task<List<FollowUp>> GetFollowUpsAsync(string currentUserId, string currentUserRole, DateTime? searchDate, FollowUpStatus? searchStatus, string? searchAssignedUser, int? customerId, int? leadId);
        Task<FollowUp?> GetFollowUpByIdAsync(int id, string currentUserId, string currentUserRole);
        Task<(bool Success, string Message, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp, string currentUserId, string currentUserRole, string ipAddress);
        Task<(bool Success, string Message)> CompleteFollowUpAsync(int id, string remarks, string currentUserId, string currentUserRole, string ipAddress);
        Task<(bool Success, string Message)> RescheduleFollowUpAsync(int id, DateTime newDate, string remarks, string currentUserId, string currentUserRole, string ipAddress);
        Task<(bool Success, string Message)> DeleteFollowUpAsync(int id, string currentUserId, string currentUserRole, string ipAddress);
        Task<List<FollowUp>> GetUpcomingFollowUpsAsync(string currentUserId, string currentUserRole);
        Task<List<FollowUp>> GetOverdueFollowUpsAsync(string currentUserId, string currentUserRole);
    }
}
