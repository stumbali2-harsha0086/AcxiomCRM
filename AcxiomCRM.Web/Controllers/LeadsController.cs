using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers
{
    [Authorize]
    public class LeadsController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadsController(ILeadService leadService, IUserService userService, UserManager<ApplicationUser> userManager)
        {
            _leadService = leadService;
            _userService = userService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? searchName, string? searchCompany, LeadStatus? searchStatus, string? searchAssignedUser)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            var leads = await _leadService.GetLeadsAsync(user.Id, role, searchName, searchCompany, searchStatus, searchAssignedUser);
            var users = await _userService.GetUsersAsync(null);

            ViewBag.SearchName = searchName;
            ViewBag.SearchCompany = searchCompany;
            ViewBag.SearchStatus = searchStatus;
            ViewBag.SearchAssignedUser = searchAssignedUser;
            ViewBag.UsersList = users;

            return View(leads);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            var lead = await _leadService.GetLeadByIdAsync(id, user.Id, role);
            if (lead == null) return NotFound();

            return View(lead);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var users = await _userService.GetUsersAsync(null);
            ViewBag.UsersList = users;
            return View(new LeadViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            if (!ModelState.IsValid)
            {
                ViewBag.UsersList = await _userService.GetUsersAsync(null);
                return View(model);
            }

            var lead = new Lead
            {
                LeadName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Source = model.Source,
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                AssignedTo = model.AssignedTo
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _leadService.CreateLeadAsync(lead, user.Id, role, ip);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.UsersList = await _userService.GetUsersAsync(null);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            var lead = await _leadService.GetLeadByIdAsync(id, user.Id, role);
            if (lead == null) return NotFound();

            var model = new LeadViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                AssignedTo = lead.AssignedTo,
                CreatedDate = lead.CreatedDate
            };

            ViewBag.UsersList = await _userService.GetUsersAsync(null);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LeadViewModel model)
        {
            if (id != model.LeadId) return BadRequest();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            if (!ModelState.IsValid)
            {
                ViewBag.UsersList = await _userService.GetUsersAsync(null);
                return View(model);
            }

            var lead = new Lead
            {
                LeadId = model.LeadId,
                LeadName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Source = model.Source,
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                AssignedTo = model.AssignedTo
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _leadService.UpdateLeadAsync(lead, user.Id, role, ip);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.UsersList = await _userService.GetUsersAsync(null);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
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
            var result = await _leadService.DeleteLeadAsync(id, user.Id, role, ip);

            if (!result.Success) TempData["ErrorMessage"] = result.Message;
            else TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Convert(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            var lead = await _leadService.GetLeadByIdAsync(id, user.Id, role);
            if (lead == null) return NotFound();

            if (lead.Status == LeadStatus.Converted)
            {
                TempData["ErrorMessage"] = "Lead is already converted.";
                return RedirectToAction(nameof(Index));
            }

            var model = new LeadConversionViewModel
            {
                LeadId = lead.LeadId,
                LeadName = lead.LeadName,
                CompanyName = lead.CompanyName,
                ExpectedValue = lead.ExpectedValue,
                CreateOpportunity = true,
                OpportunityName = $"{lead.LeadName} - Deal",
                Amount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 10000m,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(LeadConversionViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            if (model.CreateOpportunity && model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
                return View(model);
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _leadService.ConvertLeadAsync(
                model.LeadId,
                model.CreateOpportunity,
                model.OpportunityName,
                model.Amount,
                model.ExpectedCloseDate,
                user.Id,
                role,
                ip
            );

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
