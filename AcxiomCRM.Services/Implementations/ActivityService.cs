using AcxiomCRM.Data;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AcxiomCRM.Services.Implementations
{
    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public ActivityService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<List<Activity>> GetActivitiesAsync(string currentUserId, string currentUserRole, ActivityType? searchType, DateTime? searchDate, ActivityStatus? searchStatus, string? searchAssignedUser)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .Include(a => a.AssignedUser)
                .AsQueryable();

            if (currentUserRole == "Sales Executive")
            {
                query = query.Where(a => a.AssignedTo == currentUserId);
            }

            if (searchType.HasValue)
            {
                query = query.Where(a => a.ActivityType == searchType.Value);
            }

            if (searchDate.HasValue)
            {
                query = query.Where(a => a.ActivityDate.Date == searchDate.Value.Date);
            }

            if (searchStatus.HasValue)
            {
                query = query.Where(a => a.Status == searchStatus.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchAssignedUser))
            {
                query = query.Where(a => a.AssignedTo == searchAssignedUser);
            }

            return await query.OrderByDescending(a => a.ActivityDate).ToListAsync();
        }

        public async Task<Activity?> GetActivityByIdAsync(int id, string currentUserId, string currentUserRole)
        {
            var activity = await _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .Include(a => a.AssignedUser)
                .FirstOrDefaultAsync(a => a.ActivityId == id);

            if (activity == null) return null;

            if (currentUserRole == "Sales Executive" && activity.AssignedTo != currentUserId)
            {
                return null;
            }

            return activity;
        }

        public async Task<(bool Success, string Message, Activity? Activity)> CreateActivityAsync(Activity activity, string currentUserId, string currentUserRole, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(activity.Subject))
            {
                return (false, "Subject is required.", null);
            }

            if (string.IsNullOrWhiteSpace(activity.AssignedTo) || currentUserRole == "Sales Executive")
            {
                activity.AssignedTo = currentUserId;
            }

            activity.CreatedDate = DateTime.UtcNow;

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "Create",
                "Activity",
                activity.ActivityId.ToString(),
                string.Empty,
                JsonSerializer.Serialize(new { activity.ActivityType, activity.Subject, activity.AssignedTo }),
                ipAddress
            );

            return (true, "Activity logged successfully.", activity);
        }

        public async Task<(bool Success, string Message)> UpdateActivityAsync(Activity activity, string currentUserId, string currentUserRole, string ipAddress)
        {
            var existing = await _context.Activities.FindAsync(activity.ActivityId);
            if (existing == null) return (false, "Activity not found.");

            if (currentUserRole == "Sales Executive" && existing.AssignedTo != currentUserId)
            {
                return (false, "Unauthorized to modify this activity.");
            }

            if (string.IsNullOrWhiteSpace(activity.Subject))
            {
                return (false, "Subject is required.");
            }

            var oldValue = JsonSerializer.Serialize(new { existing.Subject, existing.ActivityType, existing.Status, existing.Description });

            existing.Subject = activity.Subject;
            existing.ActivityType = activity.ActivityType;
            existing.Description = activity.Description;
            existing.ActivityDate = activity.ActivityDate;
            existing.Status = activity.Status;
            existing.CustomerId = activity.CustomerId;
            existing.LeadId = activity.LeadId;
            existing.OpportunityId = activity.OpportunityId;

            _context.Activities.Update(existing);
            await _context.SaveChangesAsync();

            var newValue = JsonSerializer.Serialize(new { existing.Subject, existing.ActivityType, existing.Status, existing.Description });

            await _auditService.LogAsync(
                currentUserId,
                "Update",
                "Activity",
                existing.ActivityId.ToString(),
                oldValue,
                newValue,
                ipAddress
            );

            return (true, "Activity updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteActivityAsync(int id, string currentUserId, string currentUserRole, string ipAddress)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null) return (false, "Activity not found.");

            if (currentUserRole == "Sales Executive" && activity.AssignedTo != currentUserId)
            {
                return (false, "Unauthorized to delete this activity.");
            }

            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "Delete",
                "Activity",
                id.ToString(),
                JsonSerializer.Serialize(new { id, activity.Subject }),
                "Activity deleted",
                ipAddress
            );

            return (true, "Activity deleted successfully.");
        }
    }
}
