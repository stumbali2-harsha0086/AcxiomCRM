using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AcxiomCRM.Web.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(IReportService reportService, UserManager<ApplicationUser> userManager)
        {
            _reportService = reportService;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> CustomerReport(string? status, DateTime? fromDate, DateTime? toDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";

            var report = await _reportService.GetCustomerReportAsync(user.Id, role, status, fromDate, toDate);
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            return View(report);
        }

        public async Task<IActionResult> LeadReport(LeadStatus? status, string? source, DateTime? fromDate, DateTime? toDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";

            var report = await _reportService.GetLeadReportAsync(user.Id, role, status, source, fromDate, toDate);
            ViewBag.Status = status;
            ViewBag.Source = source;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            return View(report);
        }

        public async Task<IActionResult> FollowUpReport(FollowUpStatus? status, DateTime? fromDate, DateTime? toDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";

            var report = await _reportService.GetFollowUpReportAsync(user.Id, role, status, fromDate, toDate);
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            return View(report);
        }

        public async Task<IActionResult> OpportunityReport(OpportunityStage? stage, OpportunityStatus? status, DateTime? fromDate, DateTime? toDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";

            var report = await _reportService.GetOpportunityReportAsync(user.Id, role, stage, status, fromDate, toDate);
            ViewBag.Stage = stage;
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            return View(report);
        }

        public async Task<IActionResult> PipelineReport(string groupBy = "stage")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";

            var report = await _reportService.GetPipelineReportAsync(user.Id, role, groupBy);
            ViewBag.GroupBy = groupBy;
            return View(report);
        }

        public async Task<IActionResult> ConversionReport(DateTime? fromDate, DateTime? toDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";

            var report = await _reportService.GetConversionReportAsync(user.Id, role, fromDate, toDate);
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            return View(report);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UserActivityReport()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";

            var report = await _reportService.GetUserActivityReportAsync(user.Id, role);
            return View(report);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AuditReport(string? module, string? action, DateTime? fromDate, DateTime? toDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";

            var report = await _reportService.GetAuditReportAsync(user.Id, role, module, action, fromDate, toDate);
            ViewBag.Module = module;
            ViewBag.Action = action;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            return View(report);
        }
    }
}
