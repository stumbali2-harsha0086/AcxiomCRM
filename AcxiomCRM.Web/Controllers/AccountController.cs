using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IAuditService _auditService;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login credentials.");
                await _auditService.LogAsync("Anonymous", "Failed Login", "User", model.Email, string.Empty, "User not found", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact an administrator.");
                await _auditService.LogAsync(user.Id, "Failed Login", "User", user.Id, string.Empty, "Account deactivated", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                user.FailedLoginCount = 0;
                await _userManager.UpdateAsync(user);

                await _auditService.LogAsync(
                    user.Id,
                    "Login",
                    "User",
                    user.Id,
                    string.Empty,
                    "Successful Login",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
                );

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account locked out due to multiple failed login attempts. Please try again later or contact an administrator.");
                await _auditService.LogAsync(
                    user.Id,
                    "Lockout Event",
                    "User",
                    user.Id,
                    "Active",
                    "Account Locked Out",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
                );
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            await _auditService.LogAsync(
                user.Id,
                "Failed Login",
                "User",
                user.Id,
                string.Empty,
                "Invalid password",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
            );

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                var role = string.IsNullOrWhiteSpace(model.Role) ? "Sales Executive" : model.Role;
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    role = "Sales Executive";
                }

                await _userManager.AddToRoleAsync(user, role);

                await _auditService.LogAsync(
                    user.Id,
                    "Register",
                    "User",
                    user.Id,
                    string.Empty,
                    $"Registered user with role {role}",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
                );

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                await _auditService.LogAsync(
                    userId,
                    "Logout",
                    "User",
                    userId,
                    string.Empty,
                    "Successful Logout",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
                );
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
