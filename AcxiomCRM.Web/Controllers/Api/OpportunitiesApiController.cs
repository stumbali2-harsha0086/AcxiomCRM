using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api
{
    public class OpportunityApiDto
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int Probability { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal WeightedValue { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateOpportunityApiRequest
    {
        public string OpportunityName { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public decimal Amount { get; set; }
        public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;
        public int Probability { get; set; } = 10;
        public DateTime ExpectedCloseDate { get; set; }
    }

    [ApiController]
    [Route("api/opportunities")]
    [Authorize]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly IOpportunityService _opportunityService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpportunitiesApiController(IOpportunityService opportunityService, UserManager<ApplicationUser> userManager)
        {
            _opportunityService = opportunityService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetOpportunities([FromQuery] string? name, [FromQuery] OpportunityStage? stage, [FromQuery] OpportunityStatus? status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";
            var opps = await _opportunityService.GetOpportunitiesAsync(user.Id, role, name, null, stage, status);

            var dtos = opps.Select(o => new OpportunityApiDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.CustomerName ?? string.Empty,
                Amount = o.Amount,
                Stage = o.Stage.ToString(),
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status.ToString(),
                WeightedValue = o.WeightedValue,
                CreatedDate = o.CreatedDate
            });

            return Ok(dtos);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOpportunity([FromBody] CreateOpportunityApiRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";

            if (request.Amount <= 0) return BadRequest(new { message = "Opportunity Amount must be greater than 0." });
            if (request.Probability < 0 || request.Probability > 100) return BadRequest(new { message = "Probability must be between 0 and 100." });
            if (request.ExpectedCloseDate.Date < DateTime.UtcNow.Date) return BadRequest(new { message = "Expected Close Date cannot be in the past." });

            var opp = new Opportunity
            {
                OpportunityName = request.OpportunityName,
                CustomerId = request.CustomerId,
                LeadId = request.LeadId,
                Amount = request.Amount,
                Stage = request.Stage,
                Probability = request.Probability,
                ExpectedCloseDate = request.ExpectedCloseDate
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _opportunityService.CreateOpportunityAsync(opp, user.Id, role, ip);

            if (!result.Success) return BadRequest(new { message = result.Message });

            var dto = new OpportunityApiDto
            {
                OpportunityId = result.Opportunity!.OpportunityId,
                OpportunityName = result.Opportunity.OpportunityName,
                CustomerId = result.Opportunity.CustomerId,
                Amount = result.Opportunity.Amount,
                Stage = result.Opportunity.Stage.ToString(),
                Probability = result.Opportunity.Probability,
                ExpectedCloseDate = result.Opportunity.ExpectedCloseDate,
                Status = result.Opportunity.Status.ToString(),
                WeightedValue = result.Opportunity.WeightedValue,
                CreatedDate = result.Opportunity.CreatedDate
            };

            return Created($"/api/opportunities/{dto.OpportunityId}", dto);
        }
    }
}
