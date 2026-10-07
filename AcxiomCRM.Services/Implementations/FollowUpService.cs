using AcxiomCRM.Data;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AcxiomCRM.Services.Implementations
{
    public class FollowUpService : IFollowUpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public FollowUpService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<List<FollowUp>> GetFollowUpsAsync(string currentUserId, string currentUserRole, DateTime? searchDate, FollowUpStatus? searchStatus, string? searchAssignedUser, int? customerId, int? leadId)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedUser)
                .AsQueryable();

            if (currentUserRole == "Sales Executive")
            {
                query = query.Where(f => f.AssignedTo == currentUserId);
            }

            if (searchDate.HasValue)
            {
                query = query.Where(f => f.FollowUpDate.Date == searchDate.Value.Date);
            }

            if (searchStatus.HasValue)
            {
                query = query.Where(f => f.Status == searchStatus.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchAssignedUser))
            {
                query = query.Where(f => f.AssignedTo == searchAssignedUser);
            }

            if (customerId.HasValue)
            {
                query = query.Where(f => f.CustomerId == customerId.Value);
            }

            if (leadId.HasValue)
            {
                query = query.Where(f => f.LeadId == leadId.Value);
            }

            return await query.OrderByDescending(f => f.FollowUpDate).ToListAsync();
        }

        public async Task<FollowUp?> GetFollowUpByIdAsync(int id, string currentUserId, string currentUserRole)
        {
            var followUp = await _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedUser)
                .FirstOrDefaultAsync(f => f.FollowUpId == id);

            if (followUp == null) return null;

            if (currentUserRole == "Sales Executive" && followUp.AssignedTo != currentUserId)
            {
                return null;
            }

            return followUp;
        }

        public async Task<(bool Success, string Message, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp, string currentUserId, string currentUserRole, string ipAddress)
        {
            // Business Validation Rule: Follow-up date cannot be earlier than today for a new/planned follow-up
            if (followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
            {
                return (false, "Follow-up date cannot be earlier than today.", null);
            }

            if (string.IsNullOrWhiteSpace(followUp.AssignedTo) || currentUserRole == "Sales Executive")
            {
                followUp.AssignedTo = currentUserId;
            }

            followUp.CreatedDate = DateTime.UtcNow;
            followUp.Status = FollowUpStatus.Planned;

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "Create",
                "FollowUp",
                followUp.FollowUpId.ToString(),
                string.Empty,
                JsonSerializer.Serialize(new { followUp.FollowUpDate, followUp.FollowUpType, followUp.AssignedTo, followUp.Remarks }),
                ipAddress
            );

            return (true, "Follow-up scheduled successfully.", followUp);
        }

        public async Task<(bool Success, string Message)> CompleteFollowUpAsync(int id, string remarks, string currentUserId, string currentUserRole, string ipAddress)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp == null) return (false, "Follow-up not found.");

            if (currentUserRole == "Sales Executive" && followUp.AssignedTo != currentUserId)
            {
                return (false, "Unauthorized to update this follow-up.");
            }

            var oldStatus = followUp.Status.ToString();
            followUp.Status = FollowUpStatus.Completed;
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks) ? remarks : $"{followUp.Remarks} | Completion Note: {remarks}";
            }

            _context.FollowUps.Update(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "Complete",
                "FollowUp",
                id.ToString(),
                JsonSerializer.Serialize(new { Status = oldStatus }),
                JsonSerializer.Serialize(new { Status = "Completed", Remarks = followUp.Remarks }),
                ipAddress
            );

            return (true, "Follow-up marked as completed.");
        }

        public async Task<(bool Success, string Message)> RescheduleFollowUpAsync(int id, DateTime newDate, string remarks, string currentUserId, string currentUserRole, string ipAddress)
        {
            if (newDate.Date < DateTime.UtcNow.Date)
            {
                return (false, "Follow-up date cannot be earlier than today.");
            }

            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp == null) return (false, "Follow-up not found.");

            if (currentUserRole == "Sales Executive" && followUp.AssignedTo != currentUserId)
            {
                return (false, "Unauthorized to reschedule this follow-up.");
            }

            var oldDate = followUp.FollowUpDate.ToString("yyyy-MM-dd");
            followUp.FollowUpDate = newDate;
            followUp.Status = FollowUpStatus.Planned;
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                followUp.Remarks = $"{followUp.Remarks} | Rescheduled to {newDate:yyyy-MM-dd}: {remarks}";
            }

            _context.FollowUps.Update(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "Reschedule",
                "FollowUp",
                id.ToString(),
                JsonSerializer.Serialize(new { FollowUpDate = oldDate }),
                JsonSerializer.Serialize(new { FollowUpDate = newDate.ToString("yyyy-MM-dd"), Remarks = followUp.Remarks }),
                ipAddress
            );

            return (true, "Follow-up rescheduled successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteFollowUpAsync(int id, string currentUserId, string currentUserRole, string ipAddress)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp == null) return (false, "Follow-up not found.");

            if (currentUserRole == "Sales Executive" && followUp.AssignedTo != currentUserId)
            {
                return (false, "Unauthorized to delete this follow-up.");
            }

            _context.FollowUps.Remove(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "Delete",
                "FollowUp",
                id.ToString(),
                JsonSerializer.Serialize(new { id }),
                "FollowUp deleted",
                ipAddress
            );

            return (true, "Follow-up deleted successfully.");
        }

        public async Task<List<FollowUp>> GetUpcomingFollowUpsAsync(string currentUserId, string currentUserRole)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedUser)
                .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate.Date >= DateTime.UtcNow.Date);

            if (currentUserRole == "Sales Executive")
            {
                query = query.Where(f => f.AssignedTo == currentUserId);
            }

            return await query.OrderBy(f => f.FollowUpDate).ToListAsync();
        }

        public async Task<List<FollowUp>> GetOverdueFollowUpsAsync(string currentUserId, string currentUserRole)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedUser)
                .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate.Date < DateTime.UtcNow.Date);

            if (currentUserRole == "Sales Executive")
            {
                query = query.Where(f => f.AssignedTo == currentUserId);
            }

            return await query.OrderBy(f => f.FollowUpDate).ToListAsync();
        }
    }
}
