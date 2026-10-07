using AcxiomCRM.Data;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AcxiomCRM.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public UserService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ApplicationDbContext context, IAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _auditService = auditService;
        }

        public async Task<List<UserDto>> GetUsersAsync(string? search)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.FullName.ToLower().Contains(search.ToLower()) || u.Email!.ToLower().Contains(search.ToLower()));
            }

            var users = await query.OrderByDescending(u => u.CreatedDate).ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var roleName = roles.FirstOrDefault() ?? "Sales Executive";
                var isLockedOut = await _userManager.IsLockedOutAsync(u);

                userDtos.Add(new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? string.Empty,
                    Role = roleName,
                    IsActive = u.IsActive,
                    IsLockedOut = isLockedOut,
                    LockoutEnd = u.LockoutEnd,
                    FailedLoginCount = u.AccessFailedCount,
                    CreatedDate = u.CreatedDate
                });
            }

            return userDtos;
        }

        public async Task<UserDto?> GetUserByIdAsync(string id)
        {
            var u = await _userManager.FindByIdAsync(id);
            if (u == null) return null;

            var roles = await _userManager.GetRolesAsync(u);
            var roleName = roles.FirstOrDefault() ?? "Sales Executive";
            var isLockedOut = await _userManager.IsLockedOutAsync(u);

            return new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                Role = roleName,
                IsActive = u.IsActive,
                IsLockedOut = isLockedOut,
                LockoutEnd = u.LockoutEnd,
                FailedLoginCount = u.AccessFailedCount,
                CreatedDate = u.CreatedDate
            };
        }

        public async Task<(bool Success, string Message, string? UserId)> CreateUserAsync(string fullName, string email, string password, string role, string currentUserId, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return (false, "Full Name is required.", null);
            if (string.IsNullOrWhiteSpace(email)) return (false, "Email is required.", null);
            if (string.IsNullOrWhiteSpace(password)) return (false, "Password is required.", null);

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                return (false, "A user with this email address already exists.", null);
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                return (false, $"Role '{role}' does not exist.", null);
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return (false, errors, null);
            }

            await _userManager.AddToRoleAsync(user, role);

            await _auditService.LogAsync(
                currentUserId,
                "Create User",
                "User",
                user.Id,
                string.Empty,
                JsonSerializer.Serialize(new { user.Email, user.FullName, Role = role }),
                ipAddress
            );

            return (true, "User created successfully.", user.Id);
        }

        public async Task<(bool Success, string Message)> UpdateUserAsync(string id, string fullName, string email, string role, bool isActive, string currentUserId, string ipAddress)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return (false, "User not found.");

            if (string.IsNullOrWhiteSpace(fullName)) return (false, "Full Name is required.");
            if (string.IsNullOrWhiteSpace(email)) return (false, "Email is required.");

            var currentRoles = await _userManager.GetRolesAsync(user);
            var oldRole = currentRoles.FirstOrDefault() ?? string.Empty;

            var oldValues = JsonSerializer.Serialize(new { user.FullName, user.Email, Role = oldRole, user.IsActive });

            user.FullName = fullName;
            user.Email = email;
            user.UserName = email;
            user.IsActive = isActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return (false, string.Join("; ", updateResult.Errors.Select(e => e.Description)));
            }

            if (oldRole != role)
            {
                if (!string.IsNullOrEmpty(oldRole))
                {
                    await _userManager.RemoveFromRoleAsync(user, oldRole);
                }
                await _userManager.AddToRoleAsync(user, role);

                await _auditService.LogAsync(
                    currentUserId,
                    "Role Change",
                    "User",
                    user.Id,
                    JsonSerializer.Serialize(new { OldRole = oldRole }),
                    JsonSerializer.Serialize(new { NewRole = role }),
                    ipAddress
                );
            }

            var newValues = JsonSerializer.Serialize(new { user.FullName, user.Email, Role = role, user.IsActive });

            await _auditService.LogAsync(
                currentUserId,
                "Update User",
                "User",
                user.Id,
                oldValues,
                newValues,
                ipAddress
            );

            return (true, "User updated successfully.");
        }

        public async Task<(bool Success, string Message)> ToggleUserStatusAsync(string id, bool isActive, string currentUserId, string ipAddress)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return (false, "User not found.");

            var oldStatus = user.IsActive;
            user.IsActive = isActive;

            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync(
                currentUserId,
                isActive ? "Activate User" : "Deactivate User",
                "User",
                id,
                JsonSerializer.Serialize(new { IsActive = oldStatus }),
                JsonSerializer.Serialize(new { IsActive = isActive }),
                ipAddress
            );

            return (true, $"User status changed to {(isActive ? "Active" : "Inactive")}.");
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(string id, string newPassword, string currentUserId, string ipAddress)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return (false, "User not found.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            // Important requirement: Never log passsword or hashes!
            await _auditService.LogAsync(
                currentUserId,
                "Password Reset",
                "User",
                id,
                "Previous Password",
                "Password reset by administrator",
                ipAddress
            );

            return (true, "Password reset successfully.");
        }

        public async Task<(bool Success, string Message)> UnlockUserAsync(string id, string currentUserId, string ipAddress)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return (false, "User not found.");

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            await _auditService.LogAsync(
                currentUserId,
                "Unlock User",
                "User",
                id,
                "Account Locked",
                "Account Unlocked by Administrator",
                ipAddress
            );

            return (true, "User account unlocked successfully.");
        }

        public async Task<List<ApplicationRole>> GetRolesAsync()
        {
            return await _roleManager.Roles.ToListAsync();
        }
    }
}
