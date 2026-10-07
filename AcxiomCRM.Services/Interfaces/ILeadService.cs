using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Services.Interfaces
{
    public interface ILeadService
    {
        Task<List<Lead>> GetLeadsAsync(string currentUserId, string currentUserRole, string? searchName, string? searchCompany, LeadStatus? searchStatus, string? searchAssignedUser);
        Task<Lead?> GetLeadByIdAsync(int id, string currentUserId, string currentUserRole);
        Task<(bool Success, string Message, Lead? Lead)> CreateLeadAsync(Lead lead, string currentUserId, string currentUserRole, string ipAddress);
        Task<(bool Success, string Message)> UpdateLeadAsync(Lead lead, string currentUserId, string currentUserRole, string ipAddress);
        Task<(bool Success, string Message)> DeleteLeadAsync(int id, string currentUserId, string currentUserRole, string ipAddress);
        Task<(bool Success, string Message, Customer? Customer, Opportunity? Opportunity)> ConvertLeadAsync(int leadId, bool createOpportunity, string? opportunityName, decimal? amount, DateTime? expectedCloseDate, string currentUserId, string currentUserRole, string ipAddress);
    }
}
