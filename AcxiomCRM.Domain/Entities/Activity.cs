using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Domain.Entities
{
    public class Activity
    {
        [Key]
        public int ActivityId { get; set; }

        [Required]
        public ActivityType ActivityType { get; set; } = ActivityType.Call;

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150)]
        public string Subject { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        public int? OpportunityId { get; set; }
        [ForeignKey("OpportunityId")]
        public virtual Opportunity? Opportunity { get; set; }

        [Required]
        public string AssignedTo { get; set; } = string.Empty;

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        public ActivityStatus Status { get; set; } = ActivityStatus.Completed;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
