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
    // Helper to get user id from JWT
    private int? GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        return int.TryParse(userIdStr, out var id) ? id : null;
    }

    // Create order (Customer) - now uses OrderDTO as input
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public IActionResult CreateOrder([FromBody] OrderDTO dto)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        if (dto.Items == null || !dto.Items.Any())
            return BadRequest("Order must contain at least one item.");

        var order = new Order
        {
            CustomerId = userId.Value,
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
            if (product == null) return BadRequest($"Product {item.ProductId} not found");

            var inventory = context.InventoryItems.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (inventory == null || inventory.Quantity < item.Quantity)
                return BadRequest($"Insufficient stock for {item.ProductId}");

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

        return Ok(ToOrderDTO(order));
    }

    // Get orders for current user (Customer)
    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public IActionResult GetMyOrders()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var orders = context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.CustomerId == userId.Value)
            .ToList()
            .Select(ToOrderDTO)
            .ToList();

        return Ok(orders);
    }

    // Get all orders (Admin/Employee)
    [HttpGet]
    [Authorize(Policy = "EmployeeOrAdmin")]
    public IActionResult GetOrders([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string? deliveryType)
    {
        var query = context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value);
        if (!string.IsNullOrEmpty(deliveryType))
            query = query.Where(o => o.DeliveryType.ToString() == deliveryType);

        var orders = query.ToList().Select(ToOrderDTO).ToList();
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

        var userId = GetUserId();
        var isAdminOrEmployee = User.IsInRole("Admin") || User.IsInRole("Employee");
        if (!isAdminOrEmployee && order.CustomerId != userId)
            return Forbid();

        return Ok(ToOrderDTO(order));
    }

    // Delete order (Customer: only own, Employee/Admin: any)
    [HttpDelete("{id}")]
    [Authorize]
    public IActionResult DeleteOrder(int id)
    {
        var order = context.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.Id == id);
        if (order == null) return NotFound();

        var userId = GetUserId();
        var isAdminOrEmployee = User.IsInRole("Admin") || User.IsInRole("Employee");
        if (!isAdminOrEmployee && order.CustomerId != userId)
            return Forbid();

        context.Orders.Remove(order);
        context.SaveChanges();
        return Ok("Order deleted successfully.");
    }

    // Cancel order (Customer: only own, only if not dispatched)
    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Customer")]
    public IActionResult CancelOrder(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var order = context.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.Id == id && o.CustomerId == userId);
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
        PaymentStatus = order.PaymentStatus.ToString(),
        DispatchStatus = order.DispatchStatus.ToString(),
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