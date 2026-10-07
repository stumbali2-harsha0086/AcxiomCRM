using AcxiomCRM.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Lead> Leads { get; set; } = null!;
        public DbSet<Opportunity> Opportunities { get; set; } = null!;
        public DbSet<FollowUp> FollowUps { get; set; } = null!;
        public DbSet<Activity> Activities { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customer constraints
            builder.Entity<Customer>()
                .HasIndex(c => c.Email)
                .IsUnique();

            builder.Entity<Customer>()
                .HasIndex(c => c.Phone)
                .IsUnique();

            builder.Entity<Customer>()
                .HasIndex(c => c.CustomerCode)
                .IsUnique();

            builder.Entity<Customer>()
                .HasOne(c => c.Owner)
                .WithMany(u => u.Customers)
                .HasForeignKey(c => c.CreatedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Lead constraints
            builder.Entity<Lead>()
                .HasIndex(l => l.LeadCode)
                .IsUnique();

            builder.Entity<Lead>()
                .HasOne(l => l.AssignedUser)
                .WithMany(u => u.Leads)
                .HasForeignKey(l => l.AssignedTo)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Opportunity constraints
            builder.Entity<Opportunity>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Opportunities)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Opportunity>()
                .HasOne(o => o.Lead)
                .WithMany(l => l.Opportunities)
                .HasForeignKey(o => o.LeadId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Opportunity>()
                .HasOne(o => o.AssignedUser)
                .WithMany(u => u.Opportunities)
                .HasForeignKey(o => o.AssignedTo)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // FollowUp constraints
            builder.Entity<FollowUp>()
                .HasOne(f => f.Customer)
                .WithMany(c => c.FollowUps)
                .HasForeignKey(f => f.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FollowUp>()
                .HasOne(f => f.Lead)
                .WithMany(l => l.FollowUps)
                .HasForeignKey(f => f.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FollowUp>()
                .HasOne(f => f.AssignedUser)
                .WithMany(u => u.FollowUps)
                .HasForeignKey(f => f.AssignedTo)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Activity constraints
            builder.Entity<Activity>()
                .HasOne(a => a.Customer)
                .WithMany(c => c.Activities)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Activity>()
                .HasOne(a => a.Lead)
                .WithMany(l => l.Activities)
                .HasForeignKey(a => a.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Activity>()
                .HasOne(a => a.AssignedUser)
                .WithMany(u => u.Activities)
                .HasForeignKey(a => a.AssignedTo)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // AuditLog constraints
            builder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
