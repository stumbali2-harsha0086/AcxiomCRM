using AcxiomCRM.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            await context.Database.EnsureCreatedAsync();

            // Seed Roles
            string[] roles = new[] { "Admin", "Manager", "Sales Executive" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new ApplicationRole(roleName));
                }
            }

            // Seed Admin User
            var adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@acxiomcrm.com",
                    Email = "admin@acxiomcrm.com",
                    EmailConfirmed = true,
                    FullName = "System Admin",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed Manager User
            var managerUser = await userManager.FindByEmailAsync("manager@acxiomcrm.com");
            if (managerUser == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = "manager@acxiomcrm.com",
                    Email = "manager@acxiomcrm.com",
                    EmailConfirmed = true,
                    FullName = "Sales Manager",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(managerUser, "Manager@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, "Manager");
                }
            }

            // Seed Sales Executive User
            var salesUser = await userManager.FindByEmailAsync("sales@acxiomcrm.com");
            if (salesUser == null)
            {
                salesUser = new ApplicationUser
                {
                    UserName = "sales@acxiomcrm.com",
                    Email = "sales@acxiomcrm.com",
                    EmailConfirmed = true,
                    FullName = "Sales Exec",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(salesUser, "Sales@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(salesUser, "Sales Executive");
                }
            }

            // Reload user IDs
            adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.com");
            managerUser = await userManager.FindByEmailAsync("manager@acxiomcrm.com");
            salesUser = await userManager.FindByEmailAsync("sales@acxiomcrm.com");

            if (adminUser == null || salesUser == null || managerUser == null) return;

            // Seed Customers
            if (!context.Customers.Any())
            {
                var customers = new List<Customer>
                {
                    new Customer
                    {
                        CustomerCode = "CUST-0001",
                        CustomerName = "Acme Global Solutions",
                        Email = "contact@acmeglobal.com",
                        Phone = "9876543210",
                        CompanyName = "Acme Corp",
                        Address = "100 Tech Park",
                        City = "Mumbai",
                        State = "Maharashtra",
                        Status = "Active",
                        CreatedBy = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-30)
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-0002",
                        CustomerName = "Starlight Enterprises",
                        Email = "info@starlight.com",
                        Phone = "9876543211",
                        CompanyName = "Starlight Inc",
                        Address = "45 Commerce Way",
                        City = "Bengaluru",
                        State = "Karnataka",
                        Status = "Active",
                        CreatedBy = managerUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-20)
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-0003",
                        CustomerName = "Apex Financial Technologies",
                        Email = "sales@apexfin.com",
                        Phone = "9876543212",
                        CompanyName = "Apex Financial",
                        Address = "12 Finance Blvd",
                        City = "Delhi",
                        State = "Delhi",
                        Status = "Active",
                        CreatedBy = adminUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-10)
                    }
                };
                context.Customers.AddRange(customers);
                await context.SaveChangesAsync();
            }

            // Seed Leads
            if (!context.Leads.Any())
            {
                var leads = new List<Lead>
                {
                    new Lead
                    {
                        LeadCode = "LEAD-0001",
                        LeadName = "Robert Langdon",
                        Email = "robert@harvard.edu",
                        Phone = "9123456780",
                        CompanyName = "Symbology Labs",
                        Source = "Website",
                        Status = LeadStatus.New,
                        Priority = "High",
                        ExpectedValue = 50000m,
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-15)
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-0002",
                        LeadName = "Sarah Connor",
                        Email = "sarah@cyberdyne.com",
                        Phone = "9123456781",
                        CompanyName = "Cyberdyne Systems",
                        Source = "Referral",
                        Status = LeadStatus.Qualified,
                        Priority = "High",
                        ExpectedValue = 120000m,
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-12)
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-0003",
                        LeadName = "Bruce Wayne",
                        Email = "bruce@wayneent.com",
                        Phone = "9123456782",
                        CompanyName = "Wayne Enterprises",
                        Source = "Trade Show",
                        Status = LeadStatus.Contacted,
                        Priority = "Medium",
                        ExpectedValue = 250000m,
                        AssignedTo = managerUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-8)
                    }
                };
                context.Leads.AddRange(leads);
                await context.SaveChangesAsync();
            }

            // Seed Opportunities
            if (!context.Opportunities.Any())
            {
                var cust1 = context.Customers.First(c => c.CustomerCode == "CUST-0001");
                var cust2 = context.Customers.First(c => c.CustomerCode == "CUST-0002");

                var opportunities = new List<Opportunity>
                {
                    new Opportunity
                    {
                        OpportunityName = "Acme Cloud ERP Upgrade",
                        CustomerId = cust1.CustomerId,
                        Amount = 75000m,
                        Stage = OpportunityStage.Proposal,
                        Probability = 60,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(15),
                        Status = OpportunityStatus.Open,
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-10)
                    },
                    new Opportunity
                    {
                        OpportunityName = "Starlight Analytics License",
                        CustomerId = cust2.CustomerId,
                        Amount = 45000m,
                        Stage = OpportunityStage.Negotiation,
                        Probability = 80,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(7),
                        Status = OpportunityStatus.Open,
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-5)
                    },
                    new Opportunity
                    {
                        OpportunityName = "Apex Security Audit Project",
                        CustomerId = cust1.CustomerId,
                        Amount = 110000m,
                        Stage = OpportunityStage.Won,
                        Probability = 100,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-2),
                        Status = OpportunityStatus.Won,
                        AssignedTo = managerUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-25)
                    }
                };
                context.Opportunities.AddRange(opportunities);
                await context.SaveChangesAsync();
            }

            // Seed FollowUps
            if (!context.FollowUps.Any())
            {
                var cust1 = context.Customers.First(c => c.CustomerCode == "CUST-0001");
                var lead1 = context.Leads.First(l => l.LeadCode == "LEAD-0001");

                var followups = new List<FollowUp>
                {
                    new FollowUp
                    {
                        CustomerId = cust1.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(2),
                        FollowUpType = FollowUpType.Meeting,
                        Remarks = "Discuss Cloud ERP proposal feedback",
                        Status = FollowUpStatus.Planned,
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-1)
                    },
                    new FollowUp
                    {
                        LeadId = lead1.LeadId,
                        FollowUpDate = DateTime.UtcNow.AddDays(1),
                        FollowUpType = FollowUpType.Call,
                        Remarks = "Initial discovery call regarding Symbology requirements",
                        Status = FollowUpStatus.Planned,
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-2)
                    }
                };
                context.FollowUps.AddRange(followups);
                await context.SaveChangesAsync();
            }

            // Seed Activities
            if (!context.Activities.Any())
            {
                var cust1 = context.Customers.First(c => c.CustomerCode == "CUST-0001");
                var activities = new List<Activity>
                {
                    new Activity
                    {
                        ActivityType = ActivityType.Call,
                        Subject = "Introductory Call with Acme CTO",
                        Description = "Discussed infrastructure scope and timelines.",
                        ActivityDate = DateTime.UtcNow.AddDays(-5),
                        CustomerId = cust1.CustomerId,
                        AssignedTo = salesUser.Id,
                        Status = ActivityStatus.Completed,
                        CreatedDate = DateTime.UtcNow.AddDays(-5)
                    }
                };
                context.Activities.AddRange(activities);
                await context.SaveChangesAsync();
            }

            // Seed AuditLogs
            if (!context.AuditLogs.Any())
            {
                context.AuditLogs.Add(new AuditLog
                {
                    UserId = adminUser.Id,
                    Action = "System Initialized",
                    EntityName = "System",
                    RecordId = "0",
                    OldValue = string.Empty,
                    NewValue = "Database seeded successfully",
                    CreatedDate = DateTime.UtcNow,
                    IpAddress = "127.0.0.1"
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
