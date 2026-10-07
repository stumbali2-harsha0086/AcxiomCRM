using AcxiomCRM.Data;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AcxiomCRM.Tests
{
    public class AcceptanceTests
    {
        private ApplicationDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Test5_CreateOpportunity_AmountZero_Rejected()
        {
            var context = GetDbContext(Guid.NewGuid().ToString());
            var auditService = new AuditService(context);
            var oppService = new OpportunityService(context, auditService);

            var opp = new Opportunity
            {
                OpportunityName = "Invalid Deal",
                Amount = 0, // Invalid: <= 0
                Probability = 50,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(10)
            };

            var result = await oppService.CreateOpportunityAsync(opp, "user1", "Sales Executive", "127.0.0.1");

            Assert.False(result.Success);
            Assert.Equal("Opportunity Amount must be greater than 0.", result.Message);
        }

        [Fact]
        public async Task Test6_CreateOpportunity_ProbabilityOver100_Rejected()
        {
            var context = GetDbContext(Guid.NewGuid().ToString());
            var auditService = new AuditService(context);
            var oppService = new OpportunityService(context, auditService);

            var opp = new Opportunity
            {
                OpportunityName = "Invalid Deal",
                Amount = 10000,
                Probability = 105, // Invalid: > 100
                ExpectedCloseDate = DateTime.UtcNow.AddDays(10)
            };

            var result = await oppService.CreateOpportunityAsync(opp, "user1", "Sales Executive", "127.0.0.1");

            Assert.False(result.Success);
            Assert.Equal("Probability must be between 0 and 100.", result.Message);
        }

        [Fact]
        public async Task Test7_CreateOpportunity_ExpectedCloseDatePast_Rejected()
        {
            var context = GetDbContext(Guid.NewGuid().ToString());
            var auditService = new AuditService(context);
            var oppService = new OpportunityService(context, auditService);

            var opp = new Opportunity
            {
                OpportunityName = "Past Deal",
                Amount = 10000,
                Probability = 50,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(-2), // Invalid: Past date for active opp
                Status = OpportunityStatus.Open
            };

            var result = await oppService.CreateOpportunityAsync(opp, "user1", "Sales Executive", "127.0.0.1");

            Assert.False(result.Success);
            Assert.Equal("Expected Close Date cannot be in the past.", result.Message);
        }

        [Fact]
        public async Task Test8_CreateFollowUp_DateBeforeToday_Rejected()
        {
            var context = GetDbContext(Guid.NewGuid().ToString());
            var auditService = new AuditService(context);
            var followUpService = new FollowUpService(context, auditService);

            var followUp = new FollowUp
            {
                FollowUpDate = DateTime.UtcNow.AddDays(-1), // Invalid: Before today
                FollowUpType = FollowUpType.Call,
                Remarks = "Past follow-up"
            };

            var result = await followUpService.CreateFollowUpAsync(followUp, "user1", "Sales Executive", "127.0.0.1");

            Assert.False(result.Success);
            Assert.Equal("Follow-up date cannot be earlier than today.", result.Message);
        }

        [Fact]
        public async Task Test9_SalesExecutive_ScopedToAssignedRecords()
        {
            var context = GetDbContext(Guid.NewGuid().ToString());
            var auditService = new AuditService(context);
            var custService = new CustomerService(context, auditService);

            // Add customer owned by user1
            context.Customers.Add(new Customer
            {
                CustomerCode = "C1",
                CustomerName = "Cust 1",
                Email = "c1@test.com",
                Phone = "1111111111",
                CreatedBy = "user1"
            });

            // Add customer owned by user2
            context.Customers.Add(new Customer
            {
                CustomerCode = "C2",
                CustomerName = "Cust 2",
                Email = "c2@test.com",
                Phone = "2222222222",
                CreatedBy = "user2"
            });
            await context.SaveChangesAsync();

            // Sales Executive user1 query
            var user1Custs = await custService.GetCustomersAsync("user1", "Sales Executive", null, null, null, null);
            Assert.Single(user1Custs);
            Assert.Equal("Cust 1", user1Custs[0].CustomerName);

            // Admin query sees all
            var adminCusts = await custService.GetCustomersAsync("admin1", "Admin", null, null, null, null);
            Assert.Equal(2, adminCusts.Count);
        }

        [Fact]
        public async Task Test12_LeadConversion_GeneratesAuditEntry()
        {
            var context = GetDbContext(Guid.NewGuid().ToString());
            var auditService = new AuditService(context);
            var leadService = new LeadService(context, auditService);

            var lead = new Lead
            {
                LeadCode = "L100",
                LeadName = "Conversion Lead",
                Email = "conv@test.com",
                Phone = "9999999999",
                Status = LeadStatus.Qualified,
                ExpectedValue = 50000,
                AssignedTo = "exec1"
            };
            context.Leads.Add(lead);
            await context.SaveChangesAsync();

            var convertResult = await leadService.ConvertLeadAsync(lead.LeadId, true, "New Conv Opp", 50000, DateTime.UtcNow.AddDays(20), "exec1", "Sales Executive", "127.0.0.1");

            Assert.True(convertResult.Success);
            Assert.NotNull(convertResult.Customer);
            Assert.NotNull(convertResult.Opportunity);

            // Verify Audit Log
            var logs = await auditService.GetAuditLogsAsync(null, null, "Lead Conversion", null, null);
            Assert.Single(logs);
            Assert.Equal("Lead", logs[0].EntityName);
        }
    }
}
