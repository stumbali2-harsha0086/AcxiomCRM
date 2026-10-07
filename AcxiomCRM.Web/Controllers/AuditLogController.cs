using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class AuditLogController : Controller
    {
        private readonly IAuditService _auditService;
        private readonly IUserService _userService;

        public AuditLogController(IAuditService auditService, IUserService userService)
        {
            _auditService = auditService;
            _userService = userService;
        }

        public async Task<IActionResult> Index(string? userId, string? module, string? action, DateTime? startDate, DateTime? endDate)
        {
            var logs = await _auditService.GetAuditLogsAsync(userId, module, action, startDate, endDate);
            var users = await _userService.GetUsersAsync(null);

            ViewBag.UserId = userId;
            ViewBag.Module = module;
            ViewBag.Action = action;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.UsersList = users;

            return View(logs);
        }
    }
}
