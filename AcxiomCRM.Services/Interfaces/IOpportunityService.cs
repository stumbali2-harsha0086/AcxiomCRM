using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IOpportunityService
    {
        Task<List<Opportunity>> GetOpportunitiesAsync(string currentUserId, string currentUserRole, string? searchName, int? searchCustomerId, OpportunityStage? searchStage, OpportunityStatus? searchStatus);
        Task<Opportunity?> GetOpportunityByIdAsync(int id, string currentUserId, string currentUserRole);
        Task<(bool Success, string Message, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity, string currentUserId, string currentUserRole, string ipAddress);
        Task<(bool Success, string Message)> UpdateOpportunityAsync(Opportunity opportunity, string currentUserId, string currentUserRole, string ipAddress);
        Task<(bool Success, string Message)> UpdateStageAsync(int id, OpportunityStage newStage, string currentUserId, string currentUserRole, string ipAddress);
        Task<(bool Success, string Message)> DeleteOpportunityAsync(int id, string currentUserId, string currentUserRole, string ipAddress);
        Task<decimal> GetWeightedPipelineValueAsync(string currentUserId, string currentUserRole);
    }
}
