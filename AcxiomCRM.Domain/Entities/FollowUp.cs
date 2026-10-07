using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Domain.Entities
{
    public class FollowUp
    {
        [Key]
        public int FollowUpId { get; set; }

        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        public int? OpportunityId { get; set; }
        [ForeignKey("OpportunityId")]
        public virtual Opportunity? Opportunity { get; set; }

        [Required(ErrorMessage = "Follow-Up Date is required.")]
        public DateTime FollowUpDate { get; set; }

        [Required]
        public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

        [StringLength(500)]
        public string Remarks { get; set; } = string.Empty;

        public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

        [Required]
        public string AssignedTo { get; set; } = string.Empty;

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
