using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(string userId, string action, string entityName, string recordId, string oldValue, string newValue, string ipAddress);
        Task<List<AuditLog>> GetAuditLogsAsync(string? userId, string? module, string? action, DateTime? startDate, DateTime? endDate);
    }
}
