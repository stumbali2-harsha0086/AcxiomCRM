using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api
{
    public class FollowUpApiDto
    {
        public int FollowUpId { get; set; }
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string FollowUpType { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
    }

    public class CreateFollowUpApiRequest
    {
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public DateTime FollowUpDate { get; set; }
        public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;
        public string Remarks { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/followups")]
    [Authorize]
    public class FollowUpsApiController : ControllerBase
    {
        private readonly IFollowUpService _followUpService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowUpsApiController(IFollowUpService followUpService, UserManager<ApplicationUser> userManager)
        {
            _followUpService = followUpService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetFollowUps([FromQuery] DateTime? date, [FromQuery] FollowUpStatus? status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";
            var followUps = await _followUpService.GetFollowUpsAsync(user.Id, role, date, status, null, null, null);

            var dtos = followUps.Select(f => new FollowUpApiDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                LeadId = f.LeadId,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType.ToString(),
                Remarks = f.Remarks,
                Status = f.Status.ToString(),
                AssignedTo = f.AssignedTo
            });

            return Ok(dtos);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFollowUp([FromBody] CreateFollowUpApiRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";

            if (request.FollowUpDate.Date < DateTime.UtcNow.Date)
            {
                return BadRequest(new { message = "Follow-up date cannot be earlier than today." });
            }

            var followUp = new FollowUp
            {
                CustomerId = request.CustomerId,
                LeadId = request.LeadId,
                FollowUpDate = request.FollowUpDate,
                FollowUpType = request.FollowUpType,
                Remarks = request.Remarks
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _followUpService.CreateFollowUpAsync(followUp, user.Id, role, ip);

            if (!result.Success) return BadRequest(new { message = result.Message });

            var dto = new FollowUpApiDto
            {
                FollowUpId = result.FollowUp!.FollowUpId,
                CustomerId = result.FollowUp.CustomerId,
                LeadId = result.FollowUp.LeadId,
                FollowUpDate = result.FollowUp.FollowUpDate,
                FollowUpType = result.FollowUp.FollowUpType.ToString(),
                Remarks = result.FollowUp.Remarks,
                Status = result.FollowUp.Status.ToString(),
                AssignedTo = result.FollowUp.AssignedTo
            };

            return Created($"/api/followups/{dto.FollowUpId}", dto);
        }
    }
}
