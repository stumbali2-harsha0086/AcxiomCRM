using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetCustomersAsync(string currentUserId, string currentUserRole, string? searchName, string? searchEmail, string? searchPhone, string? searchCompany);
        Task<Customer?> GetCustomerByIdAsync(int id, string currentUserId, string currentUserRole);
        Task<(bool Success, string Message, Customer? Customer)> CreateCustomerAsync(Customer customer, string currentUserId, string ipAddress);
        Task<(bool Success, string Message)> UpdateCustomerAsync(Customer customer, string currentUserId, string currentUserRole, string ipAddress);
        Task<(bool Success, string Message)> DeleteCustomerAsync(int id, string currentUserId, string currentUserRole, string ipAddress);
        Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null);
        Task<bool> IsPhoneUniqueAsync(string phone, int? excludeId = null);
        Task<List<Activity>> GetCustomerActivitiesAsync(int customerId, string currentUserId, string currentUserRole);
        Task<List<Opportunity>> GetCustomerOpportunitiesAsync(int customerId, string currentUserId, string currentUserRole);
    }
}
