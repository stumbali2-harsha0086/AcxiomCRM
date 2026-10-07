using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Domain.Entities
{
    public class Lead
    {
        [Key]
        public int LeadId { get; set; }

        [Required]
        [StringLength(50)]
        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead Name is required.")]
        [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
        public string LeadName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(50)]
        public string Source { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead Status is required.")]
        public LeadStatus Status { get; set; } = LeadStatus.New;

        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        [Range(0, 100000000, ErrorMessage = "Expected Value must be a valid positive number.")]
        public decimal ExpectedValue { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string AssignedTo { get; set; } = string.Empty;

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        public int? ConvertedCustomerId { get; set; }
        public int? ConvertedOpportunityId { get; set; }

        public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
