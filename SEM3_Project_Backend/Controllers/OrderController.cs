using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.DTOs;
using SEM3_Project_Backend.Model;
using System.Security.Claims;

namespace SEM3_Project_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController(AppDbContext context) : ControllerBase
{
    // Helper to get user email from JWT
    private string? GetCurrentUserEmail()
    {
        return User.Identity?.Name;
    }

    // Create order (Customer)
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public IActionResult CreateOrder([FromBody] OrderDTO dto)
    {
        var email = GetCurrentUserEmail();

        if (dto == null)
        {
            return BadRequest("Order data is required.");
        }

        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized();
        }

        var customer = context.Customers.FirstOrDefault(c => c.Email == email);
        if (customer == null)
        {
            return Unauthorized();
        }

        var userId = customer.Id;

        if (dto.Items == null || !dto.Items.Any())
        {
            return BadRequest("Order must contain at least one item.");
        }

        var order = new Order
        {
            CustomerId = userId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 0,
            PaymentStatus = PaymentStatus.Pending,
            DispatchStatus = DispatchStatus.Pending,
            DeliveryDate = DateTime.UtcNow.AddDays(5),
            DeliveryAddress = dto.DeliveryAddress ?? string.Empty,
            DeliveryType = Enum.TryParse<DeliveryType>(dto.DeliveryType, true, out var deliveryType) ? deliveryType : throw new ArgumentException("Invalid delivery type"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var orderItems = new List<OrderItem>();
        foreach (var item in dto.Items)
        {
            var product = context.Products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product == null)
            {
                Console.WriteLine($"[DEBUG] Product not found: {item.ProductId}");
                return BadRequest($"Product {item.ProductId} not found");
            }

            var inventory = context.InventoryItems.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (inventory == null || inventory.Quantity < item.Quantity)
            {
                Console.WriteLine($"[DEBUG] Insufficient stock for {item.ProductId}");
                return BadRequest($"Insufficient stock for {item.ProductId}");
            }

            inventory.Quantity -= item.Quantity;

            var orderItem = new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = product.Price,
                CreatedAt = DateTime.UtcNow
            };
            order.TotalAmount += orderItem.Price * item.Quantity;
            orderItems.Add(orderItem);
        }

        order.OrderItems = orderItems;
        context.Orders.Add(order);
        context.SaveChanges();

        Console.WriteLine($"[DEBUG] Order created successfully for customer {email} (ID: {userId})");

        return Ok(ToOrderDTO(order));
    }

    // Get orders for current user (Customer)
    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public IActionResult GetMyOrders()
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var customer = context.Customers.FirstOrDefault(c => c.Email == email);
        if (customer == null) return Unauthorized();

        var orders = context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.CustomerId == customer.Id)
            .ToList()
            .Select(ToOrderDTO)
            .ToList();

        return Ok(orders);
    }

    // Get order by id (Customer: only own, Employee/Admin: any)
    [HttpGet("{id}")]
    [Authorize]
    public IActionResult GetOrderById(int id)
    {
        var order = context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefault(o => o.Id == id);

        if (order == null) return NotFound();

        var isAdminOrEmployee = User.IsInRole("Admin") || User.IsInRole("Employee");
        if (!isAdminOrEmployee)
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email)) return Unauthorized();
            var customer = context.Customers.FirstOrDefault(c => c.Email == email);
            if (customer == null || order.CustomerId != customer.Id)
                return Forbid();
        }

        return Ok(ToOrderDTO(order));
    }

    // Delete order (Customer: only own, Employee/Admin: any)
    [HttpDelete("{id}")]
    [Authorize]
    public IActionResult DeleteOrder(int id)
    {
        var order = context.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.Id == id);
        if (order == null) return NotFound();

        var isAdminOrEmployee = User.IsInRole("Admin") || User.IsInRole("Employee");
        if (!isAdminOrEmployee)
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email)) return Unauthorized();
            var customer = context.Customers.FirstOrDefault(c => c.Email == email);
            if (customer == null || order.CustomerId != customer.Id)
                return Forbid();
        }

        context.Orders.Remove(order);
        context.SaveChanges();
        return Ok("Order deleted successfully.");
    }

    // Cancel order (Customer: only own, only if not dispatched)
    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Customer")]
    public IActionResult CancelOrder(int id)
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var customer = context.Customers.FirstOrDefault(c => c.Email == email);
        if (customer == null) return Unauthorized();

        var order = context.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.Id == id && o.CustomerId == customer.Id);
        if (order == null) return NotFound();
        if (order.DispatchStatus == DispatchStatus.Dispatched || order.DispatchStatus == DispatchStatus.Delivered)
            return BadRequest("Order cannot be cancelled after dispatch.");

        order.DispatchStatus = DispatchStatus.Cancelled;
        context.SaveChanges();
        return Ok(ToOrderDTO(order));
    }

    // Update order details (Employee/Admin)
    [HttpPut("{id}")]
    [Authorize(Policy = "EmployeeOrAdmin")]
    public IActionResult UpdateOrder(int id, [FromBody] OrderDTO updated)
    {
        var order = context.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.Id == id);
        if (order == null) return NotFound();

        order.DeliveryAddress = updated.DeliveryAddress ?? order.DeliveryAddress;
        order.DeliveryType = Enum.TryParse<DeliveryType>(updated.DeliveryType, true, out var deliveryType) ? deliveryType : order.DeliveryType;
        order.DispatchStatus = Enum.TryParse<DispatchStatus>(updated.DispatchStatus, out var ds) ? ds : order.DispatchStatus;
        order.UpdatedAt = DateTime.UtcNow;
        context.SaveChanges();
        return Ok(ToOrderDTO(order));
    }

    // Update delivery status/report (Employee)
    [HttpPut("{id}/delivery")]
    [Authorize(Roles = "Employee")]
    public IActionResult UpdateDeliveryStatus(int id, [FromBody] string newStatus)
    {
        var order = context.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.Id == id);
        if (order == null) return NotFound();
        if (!Enum.TryParse<DispatchStatus>(newStatus, true, out var parsedStatus))
            return BadRequest("Invalid dispatch status.");
        order.DispatchStatus = parsedStatus;
        order.UpdatedAt = DateTime.UtcNow;
        context.SaveChanges();
        return Ok(ToOrderDTO(order));
    }

    // Helper: Map Order to OrderDTO
    private static OrderDTO ToOrderDTO(Order order) => new()
    {
        Id = order.Id,
        OrderDate = order.OrderDate,
        TotalAmount = order.TotalAmount,
        PaymentStatus = order.PaymentStatus.ToString() ?? "Pending",
        DispatchStatus = order.DispatchStatus.ToString() ?? "Pending",
        DeliveryDate = order.DeliveryDate,
        DeliveryAddress = order.DeliveryAddress,
        DeliveryType = order.DeliveryType.ToString(),
        Items = order.OrderItems?.Select(i => new OrderItemDTO
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name,
            Quantity = i.Quantity,
            Price = i.Price
        }).ToList() ?? new()
    };
}