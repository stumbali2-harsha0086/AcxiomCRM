using AcxiomCRM.Data;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Customer>> GetCustomerReportAsync(string currentUserId, string currentUserRole, string? status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Customers.Include(c => c.Owner).AsQueryable();

            if (currentUserRole == "Sales Executive") query = query.Where(c => c.CreatedBy == currentUserId);

            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
            if (fromDate.HasValue) query = query.Where(c => c.CreatedDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(c => c.CreatedDate <= toDate.Value.AddDays(1));

            return await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
        }

        public async Task<List<Lead>> GetLeadReportAsync(string currentUserId, string currentUserRole, LeadStatus? status, string? source, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Leads.Include(l => l.AssignedUser).AsQueryable();

            if (currentUserRole == "Sales Executive") query = query.Where(l => l.AssignedTo == currentUserId);

            if (status.HasValue) query = query.Where(l => l.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(source)) query = query.Where(l => l.Source == source);
            if (fromDate.HasValue) query = query.Where(l => l.CreatedDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(l => l.CreatedDate <= toDate.Value.AddDays(1));

            return await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
        }

        public async Task<List<FollowUp>> GetFollowUpReportAsync(string currentUserId, string currentUserRole, FollowUpStatus? status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).Include(f => f.AssignedUser).AsQueryable();

            if (currentUserRole == "Sales Executive") query = query.Where(f => f.AssignedTo == currentUserId);

            if (status.HasValue) query = query.Where(f => f.Status == status.Value);
            if (fromDate.HasValue) query = query.Where(f => f.FollowUpDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(f => f.FollowUpDate <= toDate.Value.AddDays(1));

            return await query.OrderByDescending(f => f.FollowUpDate).ToListAsync();
        }

        public async Task<List<Opportunity>> GetOpportunityReportAsync(string currentUserId, string currentUserRole, OpportunityStage? stage, OpportunityStatus? status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Opportunities.Include(o => o.Customer).Include(o => o.AssignedUser).AsQueryable();

            if (currentUserRole == "Sales Executive") query = query.Where(o => o.AssignedTo == currentUserId);

            if (stage.HasValue) query = query.Where(o => o.Stage == stage.Value);
            if (status.HasValue) query = query.Where(o => o.Status == status.Value);
            if (fromDate.HasValue) query = query.Where(o => o.CreatedDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(o => o.CreatedDate <= toDate.Value.AddDays(1));

            return await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
        }

        public async Task<List<PipelineReportItem>> GetPipelineReportAsync(string currentUserId, string currentUserRole, string groupBy)
        {
            var query = _context.Opportunities.Include(o => o.AssignedUser).AsQueryable();

            if (currentUserRole == "Sales Executive") query = query.Where(o => o.AssignedTo == currentUserId);

            var opps = await query.ToListAsync();

            if (groupBy?.ToLower() == "owner")
            {
                return opps.GroupBy(o => o.AssignedUser?.FullName ?? "Unassigned")
                    .Select(g => new PipelineReportItem
                    {
                        StageOrOwner = g.Key,
                        OpportunityCount = g.Count(),
                        TotalAmount = g.Sum(o => o.Amount),
                        WeightedAmount = g.Sum(o => o.Amount * o.Probability / 100m)
                    }).ToList();
            }

            // Default stage grouping
            return opps.GroupBy(o => o.Stage.ToString())
                .Select(g => new PipelineReportItem
                {
                    StageOrOwner = g.Key,
                    OpportunityCount = g.Count(),
                    TotalAmount = g.Sum(o => o.Amount),
                    WeightedAmount = g.Sum(o => o.Amount * o.Probability / 100m)
                }).ToList();
        }

        public async Task<ConversionReportDto> GetConversionReportAsync(string currentUserId, string currentUserRole, DateTime? fromDate, DateTime? toDate)
        {
            var leadQuery = _context.Leads.AsQueryable();
            var oppQuery = _context.Opportunities.AsQueryable();

            if (currentUserRole == "Sales Executive")
            {
                leadQuery = leadQuery.Where(l => l.AssignedTo == currentUserId);
                oppQuery = oppQuery.Where(o => o.AssignedTo == currentUserId);
            }

            if (fromDate.HasValue)
            {
                leadQuery = leadQuery.Where(l => l.CreatedDate >= fromDate.Value);
                oppQuery = oppQuery.Where(o => o.CreatedDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                leadQuery = leadQuery.Where(l => l.CreatedDate <= toDate.Value.AddDays(1));
                oppQuery = oppQuery.Where(o => o.CreatedDate <= toDate.Value.AddDays(1));
            }

            var leads = await leadQuery.ToListAsync();
            var opps = await oppQuery.ToListAsync();

            int totalLeads = leads.Count;
            int convertedLeads = leads.Count(l => l.Status == LeadStatus.Converted);
            int unconvertedLeads = totalLeads - convertedLeads;
            decimal conversionRate = totalLeads > 0 ? (decimal)convertedLeads / totalLeads * 100m : 0m;

            int totalOpps = opps.Count;
            int wonOpps = opps.Count(o => o.Status == OpportunityStatus.Won);
            int lostOpps = opps.Count(o => o.Status == OpportunityStatus.Lost);
            decimal wonValue = opps.Where(o => o.Status == OpportunityStatus.Won).Sum(o => o.Amount);
            decimal winRate = totalOpps > 0 ? (decimal)wonOpps / totalOpps * 100m : 0m;

            return new ConversionReportDto
            {
                TotalLeads = totalLeads,
                ConvertedLeads = convertedLeads,
                UnconvertedLeads = unconvertedLeads,
                ConversionRate = Math.Round(conversionRate, 2),
                TotalOpportunities = totalOpps,
                WonOpportunities = wonOpps,
                LostOpportunities = lostOpps,
                WonValue = wonValue,
                WinRate = Math.Round(winRate, 2)
            };
        }

        public async Task<List<UserActivityReportItem>> GetUserActivityReportAsync(string currentUserId, string currentUserRole)
        {
            var users = await _context.Users.ToListAsync();
            var result = new List<UserActivityReportItem>();

            foreach (var user in users)
            {
                if (currentUserRole == "Sales Executive" && user.Id != currentUserId) continue;

                var custCount = await _context.Customers.CountAsync(c => c.CreatedBy == user.Id);
                var leadCount = await _context.Leads.CountAsync(l => l.AssignedTo == user.Id);
                var oppCount = await _context.Opportunities.CountAsync(o => o.AssignedTo == user.Id);
                var followUpCount = await _context.FollowUps.CountAsync(f => f.AssignedTo == user.Id);
                var actCount = await _context.Activities.CountAsync(a => a.AssignedTo == user.Id);

                result.Add(new UserActivityReportItem
                {
                    UserName = user.FullName ?? user.Email ?? "Unknown",
                    Role = currentUserRole,
                    CustomersCreated = custCount,
                    LeadsAssigned = leadCount,
                    OpportunitiesAssigned = oppCount,
                    FollowUpsCount = followUpCount,
                    ActivitiesCount = actCount
                });
            }

            return result;
        }

        public async Task<List<AuditLog>> GetAuditReportAsync(string currentUserId, string currentUserRole, string? module, string? action, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.AuditLogs.Include(a => a.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(module)) query = query.Where(a => a.EntityName.ToLower().Contains(module.ToLower()));
            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action.ToLower().Contains(action.ToLower()));
            if (fromDate.HasValue) query = query.Where(a => a.CreatedDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(a => a.CreatedDate <= toDate.Value.AddDays(1));

            return await query.OrderByDescending(a => a.CreatedDate).ToListAsync();
        }
    }
}
