using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api
{
    public class LeadApiDto
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal ExpectedValue { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateLeadApiRequest
    {
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public LeadStatus Status { get; set; } = LeadStatus.New;
        public string Priority { get; set; } = "Medium";
        public decimal ExpectedValue { get; set; }
    }

    [ApiController]
    [Route("api/leads")]
    [Authorize]
    public class LeadsApiController : ControllerBase
    {
        private readonly ILeadService _leadService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadsApiController(ILeadService leadService, UserManager<ApplicationUser> userManager)
        {
            _leadService = leadService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetLeads([FromQuery] string? name, [FromQuery] string? company, [FromQuery] LeadStatus? status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";
            var leads = await _leadService.GetLeadsAsync(user.Id, role, name, company, status, null);

            var dtos = leads.Select(l => new LeadApiDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status.ToString(),
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate
            });

            return Ok(dtos);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLead([FromBody] CreateLeadApiRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";

            if (string.IsNullOrWhiteSpace(request.LeadName)) return BadRequest(new { message = "Lead Name is required." });
            if (request.ExpectedValue < 0) return BadRequest(new { message = "Expected Value cannot be negative." });

            var lead = new Lead
            {
                LeadName = request.LeadName,
                Email = request.Email,
                Phone = request.Phone,
                CompanyName = request.CompanyName,
                Source = request.Source,
                Status = request.Status,
                Priority = request.Priority,
                ExpectedValue = request.ExpectedValue
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _leadService.CreateLeadAsync(lead, user.Id, role, ip);

            if (!result.Success) return BadRequest(new { message = result.Message });

            var dto = new LeadApiDto
            {
                LeadId = result.Lead!.LeadId,
                LeadCode = result.Lead.LeadCode,
                LeadName = result.Lead.LeadName,
                Email = result.Lead.Email,
                Phone = result.Lead.Phone,
                CompanyName = result.Lead.CompanyName,
                Source = result.Lead.Source,
                Status = result.Lead.Status.ToString(),
                Priority = result.Lead.Priority,
                ExpectedValue = result.Lead.ExpectedValue,
                CreatedDate = result.Lead.CreatedDate
            };

            return Created($"/api/leads/{dto.LeadId}", dto);
        }
    }
}
