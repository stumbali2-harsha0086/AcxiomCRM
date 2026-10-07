using AcxiomCRM.Data;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services.Implementations
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string userId, string action, string entityName, string recordId, string oldValue, string newValue, string ipAddress)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                RecordId = recordId,
                OldValue = oldValue ?? string.Empty,
                NewValue = newValue ?? string.Empty,
                CreatedDate = DateTime.UtcNow,
                IpAddress = ipAddress ?? "127.0.0.1"
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync(string? userId, string? module, string? action, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.AuditLogs.Include(a => a.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (!string.IsNullOrWhiteSpace(module))
            {
                query = query.Where(a => a.EntityName.ToLower().Contains(module.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(a => a.Action.ToLower().Contains(action.ToLower()));
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate <= endDate.Value.AddDays(1));
            }

            return await query.OrderByDescending(a => a.CreatedDate).ToListAsync();
        }
    }
}
