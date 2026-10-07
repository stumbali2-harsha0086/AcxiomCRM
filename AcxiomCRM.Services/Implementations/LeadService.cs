using AcxiomCRM.Data;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AcxiomCRM.Services.Implementations
{
    public class LeadService : ILeadService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public LeadService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<List<Lead>> GetLeadsAsync(string currentUserId, string currentUserRole, string? searchName, string? searchCompany, LeadStatus? searchStatus, string? searchAssignedUser)
        {
            var query = _context.Leads.Include(l => l.AssignedUser).AsQueryable();

            if (currentUserRole == "Sales Executive")
            {
                query = query.Where(l => l.AssignedTo == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(l => l.LeadName.ToLower().Contains(searchName.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(searchCompany))
            {
                query = query.Where(l => l.CompanyName.ToLower().Contains(searchCompany.ToLower()));
            }

            if (searchStatus.HasValue)
            {
                query = query.Where(l => l.Status == searchStatus.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchAssignedUser))
            {
                query = query.Where(l => l.AssignedTo == searchAssignedUser);
            }

            return await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
        }

        public async Task<Lead?> GetLeadByIdAsync(int id, string currentUserId, string currentUserRole)
        {
            var lead = await _context.Leads
                .Include(l => l.AssignedUser)
                .Include(l => l.Opportunities)
                .Include(l => l.FollowUps)
                .Include(l => l.Activities)
                .FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null) return null;

            if (currentUserRole == "Sales Executive" && lead.AssignedTo != currentUserId)
            {
                return null; // Forbidden
            }

            return lead;
        }

        public async Task<(bool Success, string Message, Lead? Lead)> CreateLeadAsync(Lead lead, string currentUserId, string currentUserRole, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(lead.LeadName))
            {
                return (false, "Lead Name is required.", null);
            }

            if (lead.ExpectedValue < 0)
            {
                return (false, "Expected Value cannot be negative.", null);
            }

            lead.LeadCode = $"LEAD-{DateTime.UtcNow.Ticks % 1000000:D6}";
            if (string.IsNullOrWhiteSpace(lead.AssignedTo) || currentUserRole == "Sales Executive")
            {
                lead.AssignedTo = currentUserId;
            }
            lead.CreatedDate = DateTime.UtcNow;

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "Create",
                "Lead",
                lead.LeadId.ToString(),
                string.Empty,
                JsonSerializer.Serialize(new { lead.LeadCode, lead.LeadName, lead.Status, lead.AssignedTo }),
                ipAddress
            );

            return (true, "Lead created successfully.", lead);
        }

        public async Task<(bool Success, string Message)> UpdateLeadAsync(Lead lead, string currentUserId, string currentUserRole, string ipAddress)
        {
            var existing = await _context.Leads.FindAsync(lead.LeadId);
            if (existing == null)
            {
                return (false, "Lead not found.");
            }

            if (currentUserRole == "Sales Executive" && existing.AssignedTo != currentUserId)
            {
                return (false, "Unauthorized to modify this lead.");
            }

            if (string.IsNullOrWhiteSpace(lead.LeadName))
            {
                return (false, "Lead Name is required.");
            }

            if (lead.ExpectedValue < 0)
            {
                return (false, "Expected Value cannot be negative.");
            }

            var oldValue = JsonSerializer.Serialize(new { existing.LeadName, existing.Email, existing.Phone, existing.Status, existing.Priority, existing.ExpectedValue, existing.AssignedTo });

            existing.LeadName = lead.LeadName;
            existing.Email = lead.Email;
            existing.Phone = lead.Phone;
            existing.CompanyName = lead.CompanyName;
            existing.Source = lead.Source;
            existing.Status = lead.Status;
            existing.Priority = lead.Priority;
            existing.ExpectedValue = lead.ExpectedValue;

            if (currentUserRole != "Sales Executive" && !string.IsNullOrWhiteSpace(lead.AssignedTo))
            {
                existing.AssignedTo = lead.AssignedTo;
            }

            _context.Leads.Update(existing);
            await _context.SaveChangesAsync();

            var newValue = JsonSerializer.Serialize(new { existing.LeadName, existing.Email, existing.Phone, existing.Status, existing.Priority, existing.ExpectedValue, existing.AssignedTo });

            await _auditService.LogAsync(
                currentUserId,
                "Update",
                "Lead",
                existing.LeadId.ToString(),
                oldValue,
                newValue,
                ipAddress
            );

            return (true, "Lead updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteLeadAsync(int id, string currentUserId, string currentUserRole, string ipAddress)
        {
            var lead = await _context.Leads.FindAsync(id);
            if (lead == null)
            {
                return (false, "Lead not found.");
            }

            if (currentUserRole == "Sales Executive" && lead.AssignedTo != currentUserId)
            {
                return (false, "Unauthorized to delete this lead.");
            }

            var oldValue = JsonSerializer.Serialize(new { lead.LeadId, lead.LeadName, lead.Email });

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "Delete",
                "Lead",
                id.ToString(),
                oldValue,
                "Lead deleted",
                ipAddress
            );

            return (true, "Lead deleted successfully.");
        }

        public async Task<(bool Success, string Message, Customer? Customer, Opportunity? Opportunity)> ConvertLeadAsync(
            int leadId, bool createOpportunity, string? opportunityName, decimal? amount, DateTime? expectedCloseDate, string currentUserId, string currentUserRole, string ipAddress)
        {
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null)
            {
                return (false, "Lead not found.", null, null);
            }

            if (currentUserRole == "Sales Executive" && lead.AssignedTo != currentUserId)
            {
                return (false, "Unauthorized to convert this lead.", null, null);
            }

            if (lead.Status == LeadStatus.Converted)
            {
                return (false, "Lead is already converted.", null, null);
            }

            // Create Customer from Lead
            var customer = new Customer
            {
                CustomerCode = $"CUST-{DateTime.UtcNow.Ticks % 1000000:D6}",
                CustomerName = lead.LeadName,
                Email = string.IsNullOrWhiteSpace(lead.Email) ? $"{lead.LeadCode.ToLower()}@converted.com" : lead.Email,
                Phone = string.IsNullOrWhiteSpace(lead.Phone) ? $"9{DateTime.UtcNow.Ticks % 1000000000:D9}" : lead.Phone,
                CompanyName = lead.CompanyName,
                Status = "Active",
                CreatedBy = lead.AssignedTo,
                CreatedDate = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            Opportunity? opp = null;
            if (createOpportunity)
            {
                if (string.IsNullOrWhiteSpace(opportunityName))
                {
                    opportunityName = $"{lead.LeadName} - Opportunity";
                }

                if (!amount.HasValue || amount <= 0)
                {
                    amount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 10000m;
                }

                if (!expectedCloseDate.HasValue || expectedCloseDate.Value.Date < DateTime.UtcNow.Date)
                {
                    expectedCloseDate = DateTime.UtcNow.AddDays(30);
                }

                opp = new Opportunity
                {
                    OpportunityName = opportunityName,
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = amount.Value,
                    Stage = OpportunityStage.Qualification,
                    Probability = 20,
                    ExpectedCloseDate = expectedCloseDate.Value,
                    Status = OpportunityStatus.Open,
                    AssignedTo = lead.AssignedTo,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Opportunities.Add(opp);
                await _context.SaveChangesAsync();
            }

            // Update Lead
            var oldLeadStatus = lead.Status.ToString();
            lead.Status = LeadStatus.Converted;
            lead.ConvertedCustomerId = customer.CustomerId;
            if (opp != null) lead.ConvertedOpportunityId = opp.OpportunityId;

            _context.Leads.Update(lead);
            await _context.SaveChangesAsync();

            // Record conversion in Audit Log (Mandatory requirement!)
            await _auditService.LogAsync(
                currentUserId,
                "Lead Conversion",
                "Lead",
                lead.LeadId.ToString(),
                JsonSerializer.Serialize(new { LeadId = lead.LeadId, Status = oldLeadStatus }),
                JsonSerializer.Serialize(new { LeadId = lead.LeadId, Status = "Converted", CustomerId = customer.CustomerId, OpportunityId = opp?.OpportunityId }),
                ipAddress
            );

            return (true, "Lead converted successfully.", customer, opp);
        }
    }
}
