using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Domain.Entities
{
    public class Opportunity
    {
        [Key]
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 10;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        public DateTime ExpectedCloseDate { get; set; }

        public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string AssignedTo { get; set; } = string.Empty;

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

        [NotMapped]
        public decimal WeightedValue => Amount * Probability / 100m;
    }
}
