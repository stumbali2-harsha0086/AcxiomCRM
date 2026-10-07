using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Domain.Entities
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty; // e.g., Login, Failed Login, Logout, Create, Update, Delete, Role Change, Lead Conversion

        [StringLength(100)]
        public string EntityName { get; set; } = string.Empty; // e.g., Customer, Lead, Opportunity, User

        [StringLength(100)]
        public string RecordId { get; set; } = string.Empty;

        public string OldValue { get; set; } = string.Empty;

        public string NewValue { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string IpAddress { get; set; } = string.Empty;
    }
}
