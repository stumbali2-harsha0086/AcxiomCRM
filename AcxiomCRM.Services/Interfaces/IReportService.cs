using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Services.Interfaces
{
    public class PipelineReportItem
    {
        public string StageOrOwner { get; set; } = string.Empty;
        public int OpportunityCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class ConversionReportDto
    {
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int UnconvertedLeads { get; set; }
        public decimal ConversionRate { get; set; }
        public int TotalOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal WonValue { get; set; }
        public decimal WinRate { get; set; }
    }

    public class UserActivityReportItem
    {
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int CustomersCreated { get; set; }
        public int LeadsAssigned { get; set; }
        public int OpportunitiesAssigned { get; set; }
        public int FollowUpsCount { get; set; }
        public int ActivitiesCount { get; set; }
    }

    public interface IReportService
    {
        Task<List<Customer>> GetCustomerReportAsync(string currentUserId, string currentUserRole, string? status, DateTime? fromDate, DateTime? toDate);
        Task<List<Lead>> GetLeadReportAsync(string currentUserId, string currentUserRole, LeadStatus? status, string? source, DateTime? fromDate, DateTime? toDate);
        Task<List<FollowUp>> GetFollowUpReportAsync(string currentUserId, string currentUserRole, FollowUpStatus? status, DateTime? fromDate, DateTime? toDate);
        Task<List<Opportunity>> GetOpportunityReportAsync(string currentUserId, string currentUserRole, OpportunityStage? stage, OpportunityStatus? status, DateTime? fromDate, DateTime? toDate);
        Task<List<PipelineReportItem>> GetPipelineReportAsync(string currentUserId, string currentUserRole, string groupBy); // "stage" or "owner"
        Task<ConversionReportDto> GetConversionReportAsync(string currentUserId, string currentUserRole, DateTime? fromDate, DateTime? toDate);
        Task<List<UserActivityReportItem>> GetUserActivityReportAsync(string currentUserId, string currentUserRole);
        Task<List<AuditLog>> GetAuditReportAsync(string currentUserId, string currentUserRole, string? module, string? action, DateTime? fromDate, DateTime? toDate);
    }
}
