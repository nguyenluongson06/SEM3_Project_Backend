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

        // Category → Products (one-to-many)
        modelBuilder.Entity<Category>()
            .HasMany(c => c.Products)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

//TODO: add more info to seed data to avoid random category assignment and/or random info
public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        // Only seed if DB is empty
        if (!context.Admins.Any())
        {
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

        // Seed categories and products from CSV if needed
        var csvPath = Path.Combine(AppContext.BaseDirectory, "SeedData", "shop items.csv");
        if (File.Exists(csvPath))
        {
            var csvLines = File.ReadAllLines(csvPath);
            // Parse header
            var header = csvLines[0].Split(',');
            int catIdx = Array.FindIndex(header, h => h.Trim().ToLower().Contains("category"));
            int nameIdx = Array.FindIndex(header, h => h.Trim().ToLower().Contains("name") && h.Trim().ToLower() != "category name");
            int imgIdx = Array.FindIndex(header, h => h.Trim().ToLower().Contains("image"));

            // 1. Seed categories from CSV
            var csvCategories = csvLines.Skip(1)
                .Select(line => line.Split(',')[catIdx].Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Add missing categories
            var existingCategories = context.Categories.ToList();
            foreach (var catName in csvCategories)
            {
                if (!existingCategories.Any(c => c.Name != null && c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase)))
                {
                    var newCat = new Category
                    {
                        Name = catName,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow
                    };
                    context.Categories.Add(newCat);
                }
            }
            context.SaveChanges();

            // Refresh categories with IDs
            var categories = context.Categories.ToList();

            // 2. Seed products from CSV
            if (!context.Products.Any())
            {
                var catProductCounts = categories.ToDictionary(c => c.Id, c => 0);

                for (int i = 1; i < csvLines.Length; i++)
                {
                    var parts = csvLines[i].Split(',');
                    var categoryName = parts[catIdx].Trim();
                    var name = parts[nameIdx].Trim();
                    var imageUrl = parts.Length > imgIdx ? parts[imgIdx].Trim() : "";

                    // Find category by name (case-insensitive)
                    var cat = categories.FirstOrDefault(c =>
                        c.Name != null && c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                    if (cat == null) continue; // skip if category not found

                    // Generate product ID: 2-char cat + 5-digit number
                    catProductCounts[cat.Id]++;
                    var catCode = !string.IsNullOrEmpty(cat.Name) && cat.Name.Length >= 2
                        ? cat.Name[..2].ToUpper()
                        : "XX";
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