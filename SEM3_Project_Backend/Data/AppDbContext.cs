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
    public DbSet<InventoryItem> InventoryItems { get; set; }
    
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
        
        //Relationships
        
        // Customer → Orders
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Order → OrderItems
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // OrderItem → Product
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Order → Payment (one-to-one)
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithOne(o => o.Payment)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // ReturnOrReplacement → Order
        modelBuilder.Entity<ReturnOrReplacement>()
            .HasOne(rr => rr.Order)
            .WithMany(o => o.ReturnOrReplacements)
            .HasForeignKey(rr => rr.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // ReturnOrReplacement → Product
        modelBuilder.Entity<ReturnOrReplacement>()
            .HasOne(rr => rr.Product)
            .WithMany(p => p.ReturnOrReplacements)
            .HasForeignKey(rr => rr.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Customer → Feedbacks
        modelBuilder.Entity<Feedback>()
            .HasOne(f => f.Customer)
            .WithMany(c => c.Feedbacks)
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        //inventory item - product
        modelBuilder.Entity<InventoryItem>()
            .HasOne(ii => ii.Product)
            .WithOne(p => p.InventoryItem)
            .HasForeignKey<InventoryItem>(ii => ii.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        
        //InventoryItem.ProductId has fixed length
        modelBuilder.Entity<InventoryItem>()
            .Property(ii => ii.ProductId)
            .HasMaxLength(7)
            .IsFixedLength();
    }
}