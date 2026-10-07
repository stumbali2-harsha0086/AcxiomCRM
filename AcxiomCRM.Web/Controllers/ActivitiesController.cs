using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers
{
    [Authorize]
    public class ActivitiesController : Controller
    {
        private readonly IActivityService _activityService;
        private readonly ICustomerService _customerService;
        private readonly ILeadService _leadService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivitiesController(
            IActivityService activityService,
            ICustomerService customerService,
            ILeadService leadService,
            IUserService userService,
            UserManager<ApplicationUser> userManager)
        {
            _activityService = activityService;
            _customerService = customerService;
            _leadService = leadService;
            _userService = userService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(ActivityType? searchType, DateTime? searchDate, ActivityStatus? searchStatus, string? searchAssignedUser)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            var activities = await _activityService.GetActivitiesAsync(user.Id, role, searchType, searchDate, searchStatus, searchAssignedUser);

            ViewBag.SearchType = searchType;
            ViewBag.SearchDate = searchDate?.ToString("yyyy-MM-dd");
            ViewBag.SearchStatus = searchStatus;
            ViewBag.SearchAssignedUser = searchAssignedUser;
            ViewBag.UsersList = await _userService.GetUsersAsync(null);

            return View(activities);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            var model = new ActivityViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                ActivityDate = DateTime.UtcNow
            };

            ViewBag.CustomersList = await _customerService.GetCustomersAsync(user.Id, role, null, null, null, null);
            ViewBag.LeadsList = await _leadService.GetLeadsAsync(user.Id, role, null, null, null, null);
            ViewBag.UsersList = await _userService.GetUsersAsync(null);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            if (!ModelState.IsValid)
            {
                ViewBag.CustomersList = await _customerService.GetCustomersAsync(user.Id, role, null, null, null, null);
                ViewBag.LeadsList = await _leadService.GetLeadsAsync(user.Id, role, null, null, null, null);
                ViewBag.UsersList = await _userService.GetUsersAsync(null);
                return View(model);
            }

            var activity = new Activity
            {
                ActivityType = model.ActivityType,
                Subject = model.Subject,
                Description = model.Description,
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                Status = model.Status,
                AssignedTo = model.AssignedTo
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _activityService.CreateActivityAsync(activity, user.Id, role, ip);

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
    }
}
