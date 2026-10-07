using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportsApiController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsApiController(IReportService reportService, UserManager<ApplicationUser> userManager)
        {
            _reportService = reportService;
            _userManager = userManager;
        }

        [HttpGet("pipeline")]
        public async Task<IActionResult> GetPipelineReport([FromQuery] string groupBy = "stage")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";
            var report = await _reportService.GetPipelineReportAsync(user.Id, role, groupBy);

            return Ok(report);
        }
    }
}
