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
        
        // Remove all .OnDelete(DeleteBehavior.Cascade) and use Restrict instead
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithOne(o => o.Payment)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReturnOrReplacement>()
            .HasOne(rr => rr.Order)
            .WithMany(o => o.ReturnOrReplacements)
            .HasForeignKey(rr => rr.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReturnOrReplacement>()
            .HasOne(rr => rr.Product)
            .WithMany(p => p.ReturnOrReplacements)
            .HasForeignKey(rr => rr.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Feedback>()
            .HasOne(f => f.Customer)
            .WithMany(c => c.Feedbacks)
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryItem>()
            .HasOne(ii => ii.Product)
            .WithOne(p => p.InventoryItem)
            .HasForeignKey<InventoryItem>(ii => ii.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        
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

public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        // Only seed if DB is empty
        string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        if (!context.Admins.Any())
        {
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
            var defaultCatImg = "https://d2opxh93rbxzdn.cloudfront.net/original/2X/4/40cfa8ca1f24ac29cfebcb1460b5cafb213b6105.png";
            var existingCategories = context.Categories.ToList();
            foreach (var catName in csvCategories)
            {
                if (!existingCategories.Any(c => c.Name != null && c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase)))
                {
                    var newCat = new Category
                    {
                        Name = catName,
                        ImageUrl = defaultCatImg,
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

        // Add more users (customers, employees, admins) created in the last 30 days
        var random = new Random();
        var now = DateTime.UtcNow;
        var customerList = new List<Customer>();
        var employeeList = new List<Employee>();
        var adminList = new List<Admin>();
        for (int i = 1; i <= 10; i++)
        {
            var createdAt = now.AddDays(-random.Next(0, 30));
            var customer = new Customer
            {
                Name = $"Test Customer {i}",
                Email = $"customer{i}@example.com",
                HashedPassword = HashPassword($"customer{i}123"),
                PhoneNumber = $"01234567{i:D2}",
                Address = $"{i} Test St",
                CreatedAt = createdAt,
                ModifiedAt = createdAt
            };
            customerList.Add(customer);
        }
        for (int i = 1; i <= 3; i++)
        {
            var createdAt = now.AddDays(-random.Next(0, 30));
            var employee = new Employee
            {
                Username = $"employee{i}",
                HashedPassword = HashPassword($"employee{i}123"),
                Name = $"Test Employee {i}",
                CreatedAt = createdAt,
                ModifiedAt = createdAt
            };
            employeeList.Add(employee);
        }
        for (int i = 1; i <= 2; i++)
        {
            var createdAt = now.AddDays(-random.Next(0, 30));
            var admin = new Admin
            {
                Username = $"admin{i}",
                Password = HashPassword($"admin{i}123"),
                Name = $"Super Admin {i}",
                CreatedAt = createdAt
            };
            adminList.Add(admin);
        }
        context.Customers.AddRange(customerList);
        context.Employees.AddRange(employeeList);
        context.Admins.AddRange(adminList);
        context.SaveChanges();

        // Add orders and payments for the last 30 days
        var allCustomers = context.Customers.ToList();
        var allProducts = context.Products.ToList();
        var orderStatusList = new[] { DispatchStatus.Pending, DispatchStatus.Dispatched, DispatchStatus.Delivered, DispatchStatus.Cancelled };
        var paymentStatusList = new[] { PaymentStatus.Cleared, PaymentStatus.Pending, PaymentStatus.Rejected };
        for (int day = 0; day < 30; day++)
        {
            var orderDate = now.Date.AddDays(-day);
            int ordersToday = random.Next(3, 6);
            for (int o = 0; o < ordersToday; o++)
            {
                var customer = allCustomers[random.Next(allCustomers.Count)];
                var order = new Order
                {
                    CustomerId = customer.Id,
                    OrderDate = orderDate.AddHours(random.Next(0, 24)),
                    CreatedAt = orderDate.AddHours(random.Next(0, 24)),
                    UpdatedAt = orderDate.AddHours(random.Next(0, 24)),
                    DeliveryAddress = $"{customer.Address}, City {random.Next(1, 5)}",
                    DeliveryType = random.Next(0, 2) == 0 ? DeliveryType.Standard : DeliveryType.Express,
                    DispatchStatus = orderStatusList[random.Next(orderStatusList.Length)],
                    PaymentStatus = PaymentStatus.Cleared, // will be set by payment
                    TotalAmount = 0,
                    OrderItems = new List<OrderItem>()
                };
                int itemCount = random.Next(1, 4);
                var usedProducts = new HashSet<string>();
                for (int it = 0; it < itemCount; it++)
                {
                    var product = allProducts[random.Next(allProducts.Count)];
                    if (!usedProducts.Add(product.Id)) continue;
                    var quantity = random.Next(1, 5);
                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = quantity,
                        Price = product.Price,
                        CreatedAt = order.OrderDate
                    });
                    order.TotalAmount += product.Price * quantity;
                }
                // Payment
                var paymentStatus = paymentStatusList[random.Next(paymentStatusList.Length)];
                var payment = new Payment
                {
                    Order = order,
                    PaymentType = PaymentType.PayPal,
                    PaymentStatus = paymentStatus,
                    TransactionId = $"TXN{random.Next(100000, 999999)}",
                    Amount = order.TotalAmount,
                    PaymentDate = order.OrderDate
                };
                // Set order payment status to match payment
                order.PaymentStatus = paymentStatus;
                context.Orders.Add(order);
                context.Payments.Add(payment);
            }
        }
        context.SaveChanges();
    }
}