using Microsoft.EntityFrameworkCore;
using SEM3_Project_Backend.Model;

namespace SEM3_Project_Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    //tables
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<ReturnOrReplacement> ReturnOrReplacements { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    
    //relationships
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // OrderItem.DisplayOrderID
        modelBuilder.Entity<OrderItem>()
            .Property(o => o.DisplayOrderId)
            .HasMaxLength(16)
            .IsFixedLength();

        // ProductID (fixed 7-character code)
        modelBuilder.Entity<Product>()
            .Property(p => p.Id)
            .HasMaxLength(7)
            .IsFixedLength();

        // Enums as strings
        modelBuilder.Entity<Order>()
            .Property(o => o.PaymentStatus)
            .HasConversion<string>();
            
        modelBuilder.Entity<Order>()
            .Property(o => o.DispatchStatus)
            .HasConversion<string>();

        modelBuilder.Entity<Payment>()
            .Property(p => p.PaymentType)
            .HasConversion<string>();
            
        modelBuilder.Entity<Payment>()
            .Property(p => p.PaymentStatus)
            .HasConversion<string>();

        modelBuilder.Entity<ReturnOrReplacement>()
            .Property(r => r.RequestType)
            .HasConversion<string>();
            
        modelBuilder.Entity<ReturnOrReplacement>()
            .Property(r => r.ApprovalStatus)
            .HasConversion<string>();
    }
}