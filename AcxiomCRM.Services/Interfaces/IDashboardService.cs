namespace AcxiomCRM.Services.Interfaces
{
    public class DashboardKpiDto
    {
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }
        public int PendingFollowUps { get; set; }
    }

    public class ChartDataDto
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> Values { get; set; } = new List<decimal>();
    }

    public class DashboardDataDto
    {
        public DashboardKpiDto Kpis { get; set; } = new DashboardKpiDto();
        public ChartDataDto LeadStatusChart { get; set; } = new ChartDataDto();
        public ChartDataDto OpportunityPipelineChart { get; set; } = new ChartDataDto();
        public ChartDataDto MonthlySalesChart { get; set; } = new ChartDataDto();
    }

    public interface IDashboardService
    {
        Task<DashboardDataDto> GetDashboardDataAsync(string currentUserId, string currentUserRole, string? rangeFilter, DateTime? startDate, DateTime? endDate);
    }
}
