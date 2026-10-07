using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers
{
    [Authorize]
    public class OpportunitiesController : Controller
    {
        private readonly IOpportunityService _opportunityService;
        private readonly ICustomerService _customerService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpportunitiesController(IOpportunityService opportunityService, ICustomerService customerService, IUserService userService, UserManager<ApplicationUser> userManager)
        {
            _opportunityService = opportunityService;
            _customerService = customerService;
            _userService = userService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? searchName, int? searchCustomerId, OpportunityStage? searchStage, OpportunityStatus? searchStatus)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            var opportunities = await _opportunityService.GetOpportunitiesAsync(user.Id, role, searchName, searchCustomerId, searchStage, searchStatus);
            var customers = await _customerService.GetCustomersAsync(user.Id, role, null, null, null, null);

            ViewBag.SearchName = searchName;
            ViewBag.SearchCustomerId = searchCustomerId;
            ViewBag.SearchStage = searchStage;
            ViewBag.SearchStatus = searchStatus;
            ViewBag.CustomersList = customers;

            return View(opportunities);
        }

        public async Task<IActionResult> Pipeline()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            var opportunities = await _opportunityService.GetOpportunitiesAsync(user.Id, role, null, null, null, null);
            var weightedValue = await _opportunityService.GetWeightedPipelineValueAsync(user.Id, role);

            ViewBag.WeightedValue = weightedValue;
            return View(opportunities);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            var opportunity = await _opportunityService.GetOpportunityByIdAsync(id, user.Id, role);
            if (opportunity == null) return NotFound();

            return View(opportunity);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            ViewBag.CustomersList = await _customerService.GetCustomersAsync(user.Id, role, null, null, null, null);
            ViewBag.UsersList = await _userService.GetUsersAsync(null);

            return View(new OpportunityViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OpportunityViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            // Server-side Business Validations explicitly requested by PDF
            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CustomersList = await _customerService.GetCustomersAsync(user.Id, role, null, null, null, null);
                ViewBag.UsersList = await _userService.GetUsersAsync(null);
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityName = model.OpportunityName,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = model.Status,
                AssignedTo = model.AssignedTo
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _opportunityService.CreateOpportunityAsync(opp, user.Id, role, ip);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.CustomersList = await _customerService.GetCustomersAsync(user.Id, role, null, null, null, null);
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

            var opp = await _opportunityService.GetOpportunityByIdAsync(id, user.Id, role);
            if (opp == null) return NotFound();

            var model = new OpportunityViewModel
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                AssignedTo = opp.AssignedTo
            };

            ViewBag.CustomersList = await _customerService.GetCustomersAsync(user.Id, role, null, null, null, null);
            ViewBag.UsersList = await _userService.GetUsersAsync(null);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OpportunityViewModel model)
        {
            if (id != model.OpportunityId) return BadRequest();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (model.ExpectedCloseDate.Date < DateTime.UtcNow.Date && model.Status == OpportunityStatus.Open)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CustomersList = await _customerService.GetCustomersAsync(user.Id, role, null, null, null, null);
                ViewBag.UsersList = await _userService.GetUsersAsync(null);
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityId = model.OpportunityId,
                OpportunityName = model.OpportunityName,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = model.Status,
                AssignedTo = model.AssignedTo
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _opportunityService.UpdateOpportunityAsync(opp, user.Id, role, ip);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.CustomersList = await _customerService.GetCustomersAsync(user.Id, role, null, null, null, null);
                ViewBag.UsersList = await _userService.GetUsersAsync(null);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStage(int id, OpportunityStage newStage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Sales Executive";

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _opportunityService.UpdateStageAsync(id, newStage, user.Id, role, ip);

            if (!result.Success) TempData["ErrorMessage"] = result.Message;
            else TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(Pipeline));
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
            var result = await _opportunityService.DeleteOpportunityAsync(id, user.Id, role, ip);

            if (!result.Success) TempData["ErrorMessage"] = result.Message;
            else TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }
    }
}
