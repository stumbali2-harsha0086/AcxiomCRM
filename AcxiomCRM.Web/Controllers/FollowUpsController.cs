using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers
{
    [Authorize]
    public class FollowUpsController : Controller
    {
        private readonly IFollowUpService _followUpService;
        private readonly ICustomerService _customerService;
        private readonly ILeadService _leadService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowUpsController(
            IFollowUpService followUpService,
            ICustomerService customerService,
            ILeadService leadService,
            IUserService userService,
            UserManager<ApplicationUser> userManager)
        {
            _followUpService = followUpService;
            _customerService = customerService;
            _leadService = leadService;
            _userService = userService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(DateTime? searchDate, FollowUpStatus? searchStatus, string? searchAssignedUser, int? customerId, int? leadId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            var followUps = await _followUpService.GetFollowUpsAsync(user.Id, role, searchDate, searchStatus, searchAssignedUser, customerId, leadId);
            var upcoming = await _followUpService.GetUpcomingFollowUpsAsync(user.Id, role);
            var overdue = await _followUpService.GetOverdueFollowUpsAsync(user.Id, role);

            ViewBag.SearchDate = searchDate?.ToString("yyyy-MM-dd");
            ViewBag.SearchStatus = searchStatus;
            ViewBag.SearchAssignedUser = searchAssignedUser;
            ViewBag.UpcomingCount = upcoming.Count;
            ViewBag.OverdueCount = overdue.Count;
            ViewBag.UsersList = await _userService.GetUsersAsync(null);

            return View(followUps);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            var model = new FollowUpViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                FollowUpDate = DateTime.UtcNow.AddDays(1)
            };

            ViewBag.CustomersList = await _customerService.GetCustomersAsync(user.Id, role, null, null, null, null);
            ViewBag.LeadsList = await _leadService.GetLeadsAsync(user.Id, role, null, null, null, null);
            ViewBag.UsersList = await _userService.GetUsersAsync(null);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUpViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            // Mandatory Business Validation: Follow-up date cannot be earlier than today
            if (model.FollowUpDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CustomersList = await _customerService.GetCustomersAsync(user.Id, role, null, null, null, null);
                ViewBag.LeadsList = await _leadService.GetLeadsAsync(user.Id, role, null, null, null, null);
                ViewBag.UsersList = await _userService.GetUsersAsync(null);
                return View(model);
            }

            var followUp = new FollowUp
            {
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Remarks = model.Remarks,
                AssignedTo = model.AssignedTo
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _followUpService.CreateFollowUpAsync(followUp, user.Id, role, ip);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.CustomersList = await _customerService.GetCustomersAsync(user.Id, role, null, null, null, null);
                ViewBag.LeadsList = await _leadService.GetLeadsAsync(user.Id, role, null, null, null, null);
                ViewBag.UsersList = await _userService.GetUsersAsync(null);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string remarks)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _followUpService.CompleteFollowUpAsync(id, remarks, user.Id, role, ip);

            if (!result.Success) TempData["ErrorMessage"] = result.Message;
            else TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, DateTime newDate, string remarks)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            if (newDate.Date < DateTime.UtcNow.Date)
            {
                TempData["ErrorMessage"] = "Follow-up date cannot be earlier than today.";
                return RedirectToAction(nameof(Index));
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _followUpService.RescheduleFollowUpAsync(id, newDate, remarks, user.Id, role, ip);

            if (!result.Success) TempData["ErrorMessage"] = result.Message;
            else TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _followUpService.DeleteFollowUpAsync(id, user.Id, role, ip);

            if (!result.Success) TempData["ErrorMessage"] = result.Message;
            else TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }
    }
}
