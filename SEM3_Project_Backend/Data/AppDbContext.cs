﻿using Microsoft.EntityFrameworkCore;
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
    public DbSet<Category> Categories { get; set; }
    
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

public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        // Only seed if DB is empty
        if (!context.Admins.Any())
        {
            // Hashing function (same as in AuthController)
            string HashPassword(string password)
            {
                using var sha = System.Security.Cryptography.SHA256.Create();
                var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }

            // Add admin
            context.Admins.Add(new Admin
            {
                Username = "admin",
                Password = HashPassword("admin123"),
                Name = "Super Admin",
                CreatedAt = DateTime.UtcNow
            });

            // Add employee
            context.Employees.Add(new Employee
            {
                Username = "employee",
                HashedPassword = HashPassword("employee123"),
                Name = "Test Employee",
                Role = "Employee",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            });

            // Add customer
            context.Customers.Add(new Customer
            {
                Name = "Test Customer",
                Email = "customer@example.com",
                HashedPassword = HashPassword("customer123"),
                PhoneNumber = "0123456789",
                Address = "123 Test St",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            });

            context.SaveChanges();
        }

        // Seed categories if needed
        if (!context.Categories.Any())
        {
            context.Categories.AddRange(
                new Category { Name = "Ceramics", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow },
                new Category { Name = "Bags", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow },
                new Category { Name = "Art", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow },
                new Category { Name = "Cosmetics", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow },
                new Category { Name = "Accessories", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
            );
            context.SaveChanges();
        }

        // Seed products from CSV if needed
        if (!context.Products.Any())
        {
            var categories = context.Categories.ToList();
            var csvPath = Path.Combine(AppContext.BaseDirectory, "SeedData", "shop items.csv");
            if (File.Exists(csvPath))
            {
                var csvLines = File.ReadAllLines(csvPath);
                var catProductCounts = categories.ToDictionary(c => c.Id, c => 0);

                for (int i = 1; i < csvLines.Length; i++)
                {
                    var parts = csvLines[i].Split(',');
                    var name = parts[0];
                    var imageUrl = parts.Length > 1 ? parts[1] : "";

                    // Assign category by keyword
                    var cat = categories.FirstOrDefault(c =>
                        name.ToLower().Contains(c.Name!.ToLower().Split(' ')[0])) ?? categories[0];

                    // Generate product ID: 2-char cat + 5-digit number
                    catProductCounts[cat.Id]++;
                    var catCode = cat.Name!.Length >= 2 ? cat.Name.Substring(0, 2).ToUpper() : "XX";
                    var prodNum = catProductCounts[cat.Id];
                    var prodId = $"{catCode}{prodNum:D5}";

                    var product = new Product
                    {
                        Id = prodId,
                        Name = name,
                        Description = $"Sample description for {name}",
                        Price = 10 + i * 2,
                        ImageUrl = imageUrl,
                        CategoryId = cat.Id,
                        WarrantyPeriod = 12,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        InventoryItem = new InventoryItem
                        {
                            ProductId = prodId,
                            Quantity = 20 + i,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    };
                    context.Products.Add(product);
                }
                context.SaveChanges();
            }
        }
    }
}