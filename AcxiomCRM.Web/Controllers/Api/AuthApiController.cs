using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public AuthApiController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditService = auditService;
        }

        public class LoginApiRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginApiRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email and Password are required." });
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { message = "Invalid email or account is inactive." });
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, request.Password, false, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                await _auditService.LogAsync(user.Id, "API Login", "User", user.Id, string.Empty, "API Authentication successful", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");

                return Ok(new
                {
                    message = "Login successful.",
                    userId = user.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    role = roles.FirstOrDefault() ?? "Sales Executive"
                });
            }

            if (result.IsLockedOut)
            {
                return StatusCode(429, new { message = "Account locked out due to multiple failed login attempts." });
            }

            return Unauthorized(new { message = "Invalid email or password." });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "Logout successful." });
        }
    }
}
