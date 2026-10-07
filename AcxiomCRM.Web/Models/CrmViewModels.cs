using AcxiomCRM.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.Models
{
    public class CustomerViewModel
    {
        public int CustomerId { get; set; }

        public string CustomerCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(100, ErrorMessage = "Customer Name cannot exceed 100 characters.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(250)]
        public string Address { get; set; } = string.Empty;

        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        public string Status { get; set; } = "Active";

        public DateTime CreatedDate { get; set; }

        public string OwnerName { get; set; } = string.Empty;
    }

    public class LeadViewModel
    {
        public int LeadId { get; set; }

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

        public string AssignedTo { get; set; } = string.Empty;
        public string AssignedUserName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class LeadConversionViewModel
    {
        public int LeadId { get; set; }
        public string LeadName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal ExpectedValue { get; set; }

        public bool CreateOpportunity { get; set; } = true;

        public string OpportunityName { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);
    }

    public class OpportunityViewModel
    {
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        public int? LeadId { get; set; }
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Opportunity Amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 10;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

        public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

        public string AssignedTo { get; set; } = string.Empty;
        public string AssignedUserName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }

        public decimal WeightedValue => Amount * Probability / 100m;
    }

    public class FollowUpViewModel
    {
        public int FollowUpId { get; set; }

        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        public int? LeadId { get; set; }
        public string LeadName { get; set; } = string.Empty;

        public int? OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Follow-Up Date is required.")]
        public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

        [Required]
        public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

        [StringLength(500)]
        public string Remarks { get; set; } = string.Empty;

        public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

        public string AssignedTo { get; set; } = string.Empty;
        public string AssignedUserName { get; set; } = string.Empty;
    }

    public class ActivityViewModel
    {
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
        public string CustomerName { get; set; } = string.Empty;

        public int? LeadId { get; set; }
        public string LeadName { get; set; } = string.Empty;

        public int? OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;

        public string AssignedTo { get; set; } = string.Empty;
        public string AssignedUserName { get; set; } = string.Empty;

        public ActivityStatus Status { get; set; } = ActivityStatus.Completed;
    }
}
