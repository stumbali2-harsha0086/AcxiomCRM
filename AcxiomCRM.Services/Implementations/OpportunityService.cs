using AcxiomCRM.Data;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AcxiomCRM.Services.Implementations
{
    public class OpportunityService : IOpportunityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public OpportunityService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<List<Opportunity>> GetOpportunitiesAsync(string currentUserId, string currentUserRole, string? searchName, int? searchCustomerId, OpportunityStage? searchStage, OpportunityStatus? searchStatus)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .AsQueryable();

            if (currentUserRole == "Sales Executive")
            {
                query = query.Where(o => o.AssignedTo == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(o => o.OpportunityName.ToLower().Contains(searchName.ToLower()));
            }

            if (searchCustomerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == searchCustomerId.Value);
            }

            if (searchStage.HasValue)
            {
                query = query.Where(o => o.Stage == searchStage.Value);
            }

            if (searchStatus.HasValue)
            {
                query = query.Where(o => o.Status == searchStatus.Value);
            }

            return await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
        }

        public async Task<Opportunity?> GetOpportunityByIdAsync(int id, string currentUserId, string currentUserRole)
        {
            var opportunity = await _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .Include(o => o.FollowUps)
                .Include(o => o.Activities)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opportunity == null) return null;

            if (currentUserRole == "Sales Executive" && opportunity.AssignedTo != currentUserId)
            {
                return null;
            }

            return opportunity;
        }

        public async Task<(bool Success, string Message, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity, string currentUserId, string currentUserRole, string ipAddress)
        {
            // Business Validations explicitly requested by PDF
            if (string.IsNullOrWhiteSpace(opportunity.OpportunityName))
            {
                return (false, "Opportunity Name is required.", null);
            }

            if (opportunity.Amount <= 0)
            {
                return (false, "Opportunity Amount must be greater than 0.", null);
            }

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
            {
                return (false, "Probability must be between 0 and 100.", null);
            }

            if (opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date && opportunity.Status == OpportunityStatus.Open)
            {
                return (false, "Expected Close Date cannot be in the past.", null);
            }

            if (string.IsNullOrWhiteSpace(opportunity.AssignedTo) || currentUserRole == "Sales Executive")
            {
                opportunity.AssignedTo = currentUserId;
            }

            opportunity.CreatedDate = DateTime.UtcNow;
            if (opportunity.Stage == OpportunityStage.Won) opportunity.Status = OpportunityStatus.Won;
            else if (opportunity.Stage == OpportunityStage.Lost) opportunity.Status = OpportunityStatus.Lost;
            else opportunity.Status = OpportunityStatus.Open;

            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "Create",
                "Opportunity",
                opportunity.OpportunityId.ToString(),
                string.Empty,
                JsonSerializer.Serialize(new { opportunity.OpportunityName, opportunity.Amount, opportunity.Stage, opportunity.Probability, opportunity.ExpectedCloseDate }),
                ipAddress
            );

            return (true, "Opportunity created successfully.", opportunity);
        }

        public async Task<(bool Success, string Message)> UpdateOpportunityAsync(Opportunity opportunity, string currentUserId, string currentUserRole, string ipAddress)
        {
            var existing = await _context.Opportunities.FindAsync(opportunity.OpportunityId);
            if (existing == null)
            {
                return (false, "Opportunity not found.");
            }

            if (currentUserRole == "Sales Executive" && existing.AssignedTo != currentUserId)
            {
                return (false, "Unauthorized to modify this opportunity.");
            }

            if (string.IsNullOrWhiteSpace(opportunity.OpportunityName))
            {
                return (false, "Opportunity Name is required.");
            }

            if (opportunity.Amount <= 0)
            {
                return (false, "Opportunity Amount must be greater than 0.");
            }

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
            {
                return (false, "Probability must be between 0 and 100.");
            }

            if (opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date && opportunity.Status == OpportunityStatus.Open)
            {
                return (false, "Expected Close Date cannot be in the past.");
            }

            var oldValue = JsonSerializer.Serialize(new { existing.OpportunityName, existing.Amount, existing.Stage, existing.Probability, existing.ExpectedCloseDate, existing.Status });

            existing.OpportunityName = opportunity.OpportunityName;
            existing.CustomerId = opportunity.CustomerId;
            existing.LeadId = opportunity.LeadId;
            existing.Amount = opportunity.Amount;
            existing.Stage = opportunity.Stage;
            existing.Probability = opportunity.Probability;
            existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;

            if (opportunity.Stage == OpportunityStage.Won) existing.Status = OpportunityStatus.Won;
            else if (opportunity.Stage == OpportunityStage.Lost) existing.Status = OpportunityStatus.Lost;
            else existing.Status = OpportunityStatus.Open;

            if (currentUserRole != "Sales Executive" && !string.IsNullOrWhiteSpace(opportunity.AssignedTo))
            {
                existing.AssignedTo = opportunity.AssignedTo;
            }

            _context.Opportunities.Update(existing);
            await _context.SaveChangesAsync();

            var newValue = JsonSerializer.Serialize(new { existing.OpportunityName, existing.Amount, existing.Stage, existing.Probability, existing.ExpectedCloseDate, existing.Status });

            await _auditService.LogAsync(
                currentUserId,
                "Update",
                "Opportunity",
                existing.OpportunityId.ToString(),
                oldValue,
                newValue,
                ipAddress
            );

            return (true, "Opportunity updated successfully.");
        }

        public async Task<(bool Success, string Message)> UpdateStageAsync(int id, OpportunityStage newStage, string currentUserId, string currentUserRole, string ipAddress)
        {
            var opportunity = await _context.Opportunities.FindAsync(id);
            if (opportunity == null) return (false, "Opportunity not found.");

            if (currentUserRole == "Sales Executive" && opportunity.AssignedTo != currentUserId)
            {
                return (false, "Unauthorized to modify this opportunity.");
            }

            var oldStage = opportunity.Stage.ToString();
            opportunity.Stage = newStage;

            if (newStage == OpportunityStage.Won)
            {
                opportunity.Status = OpportunityStatus.Won;
                opportunity.Probability = 100;
            }
            else if (newStage == OpportunityStage.Lost)
            {
                opportunity.Status = OpportunityStatus.Lost;
                opportunity.Probability = 0;
            }

            _context.Opportunities.Update(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "Stage Change",
                "Opportunity",
                id.ToString(),
                JsonSerializer.Serialize(new { Stage = oldStage }),
                JsonSerializer.Serialize(new { Stage = newStage.ToString(), Status = opportunity.Status.ToString() }),
                ipAddress
            );

            return (true, $"Opportunity stage updated to {newStage}.");
        }

        public async Task<(bool Success, string Message)> DeleteOpportunityAsync(int id, string currentUserId, string currentUserRole, string ipAddress)
        {
            var opportunity = await _context.Opportunities.FindAsync(id);
            if (opportunity == null) return (false, "Opportunity not found.");

            if (currentUserRole == "Sales Executive" && opportunity.AssignedTo != currentUserId)
            {
                return (false, "Unauthorized to delete this opportunity.");
            }

            var oldValue = JsonSerializer.Serialize(new { opportunity.OpportunityId, opportunity.OpportunityName, opportunity.Amount });

            _context.Opportunities.Remove(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "Delete",
                "Opportunity",
                id.ToString(),
                oldValue,
                "Opportunity deleted",
                ipAddress
            );

            return (true, "Opportunity deleted successfully.");
        }

        public async Task<decimal> GetWeightedPipelineValueAsync(string currentUserId, string currentUserRole)
        {
            var query = _context.Opportunities.Where(o => o.Status == OpportunityStatus.Open);
            if (currentUserRole == "Sales Executive")
            {
                query = query.Where(o => o.AssignedTo == currentUserId);
            }

            var openOpps = await query.ToListAsync();
            return openOpps.Sum(o => o.Amount * o.Probability / 100m);
        }
    }
}
