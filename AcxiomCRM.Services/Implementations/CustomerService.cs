using AcxiomCRM.Data;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AcxiomCRM.Services.Implementations
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public CustomerService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<List<Customer>> GetCustomersAsync(string currentUserId, string currentUserRole, string? searchName, string? searchEmail, string? searchPhone, string? searchCompany)
        {
            var query = _context.Customers.Include(c => c.Owner).AsQueryable();

            // Role-based authorization scoping
            if (currentUserRole == "Sales Executive")
            {
                query = query.Where(c => c.CreatedBy == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(c => c.CustomerName.ToLower().Contains(searchName.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(searchEmail))
            {
                query = query.Where(c => c.Email.ToLower().Contains(searchEmail.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(searchPhone))
            {
                query = query.Where(c => c.Phone.Contains(searchPhone));
            }

            if (!string.IsNullOrWhiteSpace(searchCompany))
            {
                query = query.Where(c => c.CompanyName.ToLower().Contains(searchCompany.ToLower()));
            }

            return await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id, string currentUserId, string currentUserRole)
        {
            var customer = await _context.Customers
                .Include(c => c.Owner)
                .Include(c => c.Opportunities)
                .Include(c => c.FollowUps)
                .Include(c => c.Activities)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return null;

            // Authorization check
            if (currentUserRole == "Sales Executive" && customer.CreatedBy != currentUserId)
            {
                return null; // Forbidden
            }

            return customer;
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(email)) return true;
            var query = _context.Customers.AsQueryable();
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeId.Value);
            }
            return !await query.AnyAsync(c => c.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(phone)) return true;
            var query = _context.Customers.AsQueryable();
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeId.Value);
            }
            return !await query.AnyAsync(c => c.Phone == phone);
        }

        public async Task<(bool Success, string Message, Customer? Customer)> CreateCustomerAsync(Customer customer, string currentUserId, string ipAddress)
        {
            // Business Validation
            if (string.IsNullOrWhiteSpace(customer.CustomerName))
            {
                return (false, "Customer Name is required.", null);
            }

            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                return (false, "Enter a valid email address.", null);
            }

            if (string.IsNullOrWhiteSpace(customer.Phone))
            {
                return (false, "Enter a valid phone number.", null);
            }

            if (!await IsEmailUniqueAsync(customer.Email))
            {
                return (false, "A customer with this email address already exists.", null);
            }

            if (!await IsPhoneUniqueAsync(customer.Phone))
            {
                return (false, "A customer with this phone number already exists.", null);
            }

            // Duplicate customer check (Name + Company)
            if (!string.IsNullOrWhiteSpace(customer.CompanyName))
            {
                var duplicateExists = await _context.Customers.AnyAsync(c =>
                    c.CustomerName.ToLower() == customer.CustomerName.ToLower() &&
                    c.CompanyName.ToLower() == customer.CompanyName.ToLower());
                if (duplicateExists)
                {
                    return (false, "A duplicate customer with the same name and company already exists.", null);
                }
            }

            customer.CustomerCode = $"CUST-{DateTime.UtcNow.Ticks % 1000000:D6}";
            customer.CreatedBy = currentUserId;
            customer.CreatedDate = DateTime.UtcNow;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "Create",
                "Customer",
                customer.CustomerId.ToString(),
                string.Empty,
                JsonSerializer.Serialize(new { customer.CustomerCode, customer.CustomerName, customer.Email, customer.Phone }),
                ipAddress
            );

            return (true, "Customer created successfully.", customer);
        }

        public async Task<(bool Success, string Message)> UpdateCustomerAsync(Customer customer, string currentUserId, string currentUserRole, string ipAddress)
        {
            var existing = await _context.Customers.FindAsync(customer.CustomerId);
            if (existing == null)
            {
                return (false, "Customer not found.");
            }

            // Authorization check
            if (currentUserRole == "Sales Executive" && existing.CreatedBy != currentUserId)
            {
                return (false, "Unauthorized to modify this customer.");
            }

            // Business Validation
            if (string.IsNullOrWhiteSpace(customer.CustomerName))
            {
                return (false, "Customer Name is required.");
            }

            if (!await IsEmailUniqueAsync(customer.Email, customer.CustomerId))
            {
                return (false, "A customer with this email address already exists.");
            }

            if (!await IsPhoneUniqueAsync(customer.Phone, customer.CustomerId))
            {
                return (false, "A customer with this phone number already exists.");
            }

            var oldValue = JsonSerializer.Serialize(new { existing.CustomerName, existing.Email, existing.Phone, existing.Address, existing.City, existing.State, existing.Status });

            existing.CustomerName = customer.CustomerName;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;
            existing.CompanyName = customer.CompanyName;
            existing.Address = customer.Address;
            existing.City = customer.City;
            existing.State = customer.State;
            existing.Status = customer.Status;

            _context.Customers.Update(existing);
            await _context.SaveChangesAsync();

            var newValue = JsonSerializer.Serialize(new { existing.CustomerName, existing.Email, existing.Phone, existing.Address, existing.City, existing.State, existing.Status });

            await _auditService.LogAsync(
                currentUserId,
                "Update",
                "Customer",
                existing.CustomerId.ToString(),
                oldValue,
                newValue,
                ipAddress
            );

            return (true, "Customer updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteCustomerAsync(int id, string currentUserId, string currentUserRole, string ipAddress)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return (false, "Customer not found.");
            }

            // Authorization check
            if (currentUserRole == "Sales Executive" && customer.CreatedBy != currentUserId)
            {
                return (false, "Unauthorized to delete this customer.");
            }

            var oldValue = JsonSerializer.Serialize(new { customer.CustomerId, customer.CustomerName, customer.Email });

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "Delete",
                "Customer",
                id.ToString(),
                oldValue,
                "Customer deleted",
                ipAddress
            );

            return (true, "Customer deleted successfully.");
        }

        public async Task<List<Activity>> GetCustomerActivitiesAsync(int customerId, string currentUserId, string currentUserRole)
        {
            var query = _context.Activities.Include(a => a.AssignedUser).Where(a => a.CustomerId == customerId);
            if (currentUserRole == "Sales Executive")
            {
                query = query.Where(a => a.AssignedTo == currentUserId);
            }
            return await query.OrderByDescending(a => a.ActivityDate).ToListAsync();
        }

        public async Task<List<Opportunity>> GetCustomerOpportunitiesAsync(int customerId, string currentUserId, string currentUserRole)
        {
            var query = _context.Opportunities.Include(o => o.AssignedUser).Where(o => o.CustomerId == customerId);
            if (currentUserRole == "Sales Executive")
            {
                query = query.Where(o => o.AssignedTo == currentUserId);
            }
            return await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
        }
    }
}
