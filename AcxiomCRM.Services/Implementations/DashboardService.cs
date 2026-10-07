using AcxiomCRM.Data;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardDataDto> GetDashboardDataAsync(string currentUserId, string currentUserRole, string? rangeFilter, DateTime? startDate, DateTime? endDate)
        {
            // Determine date filter boundaries
            DateTime? fromDate = null;
            DateTime? toDate = null;
            var now = DateTime.UtcNow;

            switch (rangeFilter?.ToLower())
            {
                case "today":
                    fromDate = now.Date;
                    toDate = now.Date.AddDays(1).AddTicks(-1);
                    break;
                case "thisweek":
                    var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
                    fromDate = startOfWeek;
                    toDate = startOfWeek.AddDays(7).AddTicks(-1);
                    break;
                case "thismonth":
                    fromDate = new DateTime(now.Year, now.Month, 1);
                    toDate = fromDate.Value.AddMonths(1).AddTicks(-1);
                    break;
                case "custom":
                    if (startDate.HasValue) fromDate = startDate.Value.Date;
                    if (endDate.HasValue) toDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    break;
            }

            // Customer Query (Scoped)
            var customerQuery = _context.Customers.AsQueryable();
            if (currentUserRole == "Sales Executive") customerQuery = customerQuery.Where(c => c.CreatedBy == currentUserId);
            if (fromDate.HasValue) customerQuery = customerQuery.Where(c => c.CreatedDate >= fromDate.Value);
            if (toDate.HasValue) customerQuery = customerQuery.Where(c => c.CreatedDate <= toDate.Value);

            // Lead Query (Scoped)
            var leadQuery = _context.Leads.AsQueryable();
            if (currentUserRole == "Sales Executive") leadQuery = leadQuery.Where(l => l.AssignedTo == currentUserId);
            if (fromDate.HasValue) leadQuery = leadQuery.Where(l => l.CreatedDate >= fromDate.Value);
            if (toDate.HasValue) leadQuery = leadQuery.Where(l => l.CreatedDate <= toDate.Value);

            // Opportunity Query (Scoped)
            var oppQuery = _context.Opportunities.AsQueryable();
            if (currentUserRole == "Sales Executive") oppQuery = oppQuery.Where(o => o.AssignedTo == currentUserId);
            if (fromDate.HasValue) oppQuery = oppQuery.Where(o => o.CreatedDate >= fromDate.Value);
            if (toDate.HasValue) oppQuery = oppQuery.Where(o => o.CreatedDate <= toDate.Value);

            // FollowUp Query (Scoped)
            var followUpQuery = _context.FollowUps.AsQueryable();
            if (currentUserRole == "Sales Executive") followUpQuery = followUpQuery.Where(f => f.AssignedTo == currentUserId);

            var customersList = await customerQuery.ToListAsync();
            var leadsList = await leadQuery.ToListAsync();
            var oppsList = await oppQuery.ToListAsync();
            var followUpsList = await followUpQuery.ToListAsync();

            var kpis = new DashboardKpiDto
            {
                TotalCustomers = customersList.Count,
                TotalLeads = leadsList.Count,
                OpenLeads = leadsList.Count(l => l.Status != LeadStatus.Converted && l.Status != LeadStatus.Lost && l.Status != LeadStatus.Unqualified),
                TotalOpportunities = oppsList.Count,
                OpenOpportunities = oppsList.Count(o => o.Status == OpportunityStatus.Open),
                WonOpportunities = oppsList.Count(o => o.Status == OpportunityStatus.Won),
                LostOpportunities = oppsList.Count(o => o.Status == OpportunityStatus.Lost),
                TotalPipelineValue = oppsList.Where(o => o.Status == OpportunityStatus.Open).Sum(o => o.Amount),
                WeightedPipelineValue = oppsList.Where(o => o.Status == OpportunityStatus.Open).Sum(o => o.Amount * o.Probability / 100m),
                PendingFollowUps = followUpsList.Count(f => f.Status == FollowUpStatus.Planned)
            };

            // Chart 1: Lead Status
            var leadStatusChart = new ChartDataDto
            {
                Labels = new List<string> { "New", "Contacted", "Qualified", "Lost", "Converted" },
                Values = new List<decimal>
                {
                    leadsList.Count(l => l.Status == LeadStatus.New),
                    leadsList.Count(l => l.Status == LeadStatus.Contacted),
                    leadsList.Count(l => l.Status == LeadStatus.Qualified),
                    leadsList.Count(l => l.Status == LeadStatus.Lost || l.Status == LeadStatus.Unqualified),
                    leadsList.Count(l => l.Status == LeadStatus.Converted)
                }
            };

            // Chart 2: Opportunity Pipeline
            var oppPipelineChart = new ChartDataDto
            {
                Labels = new List<string> { "Qualification", "Proposal", "Negotiation", "Won", "Lost" },
                Values = new List<decimal>
                {
                    oppsList.Where(o => o.Stage == OpportunityStage.Qualification).Sum(o => o.Amount),
                    oppsList.Where(o => o.Stage == OpportunityStage.Proposal).Sum(o => o.Amount),
                    oppsList.Where(o => o.Stage == OpportunityStage.Negotiation).Sum(o => o.Amount),
                    oppsList.Where(o => o.Stage == OpportunityStage.Won).Sum(o => o.Amount),
                    oppsList.Where(o => o.Stage == OpportunityStage.Lost).Sum(o => o.Amount)
                }
            };

            // Chart 3: Monthly Sales (Last 6 Months)
            var monthlyLabels = new List<string>();
            var monthlyValues = new List<decimal>();

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = DateTime.UtcNow.AddMonths(-i);
                var monthName = monthDate.ToString("MMM yyyy");
                monthlyLabels.Add(monthName);

                var monthWonTotal = oppsList
                    .Where(o => o.Status == OpportunityStatus.Won && o.CreatedDate.Month == monthDate.Month && o.CreatedDate.Year == monthDate.Year)
                    .Sum(o => o.Amount);

                monthlyValues.Add(monthWonTotal);
            }

            var monthlySalesChart = new ChartDataDto
            {
                Labels = monthlyLabels,
                Values = monthlyValues
            };

            return new DashboardDataDto
            {
                Kpis = kpis,
                LeadStatusChart = leadStatusChart,
                OpportunityPipelineChart = oppPipelineChart,
                MonthlySalesChart = monthlySalesChart
            };
        }
    }
}
