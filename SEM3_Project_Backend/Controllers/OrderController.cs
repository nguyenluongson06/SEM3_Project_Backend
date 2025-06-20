using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.DTOs;
using SEM3_Project_Backend.Model;

namespace SEM3_Project_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController(AppDbContext context) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public IActionResult CreateOrder(OrderRequest request)
    {
        var order = new Order
        {
            CustomerId = request.CustomerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 0,
            PaymentStatus = PaymentStatus.Pending,
            DispatchStatus = DispatchStatus.Pending,
            DeliveryDate = DateTime.UtcNow.AddDays(5)
        };

        var orderItems = new List<OrderItem>();
        foreach (var item in request.Items)
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

        return Ok(order);
    }

    [HttpGet("user/{userId}")]
    public IActionResult GetOrdersByUser(int userId)
    {
        var orders = context.Orders.Where(o => o.CustomerId == userId).ToList();
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public IActionResult GetOrderById(int id)
    {
        var order = context.Orders
            .Where(o => o.Id == id)
            .Select(o => new
            {
                o.Id,
                o.CustomerId,
                o.OrderDate,
                o.TotalAmount,
                Items = o.OrderItems!.Select(i => new {
                    i.ProductId,
                    i.Quantity,
                    i.Price
                })
            }).FirstOrDefault();
        return order == null ? NotFound() : Ok(order);
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteOrder(int id)
    {
        var order = context.Orders.FirstOrDefault(o => o.Id == id);
        if (order == null) return NotFound();
        context.Orders.Remove(order);
        context.SaveChanges();
        return Ok();
    }

    // Customer: Cancel order if not dispatched
    [HttpPut("{id}/cancel")]
    [Authorize(Policy = "Customer")]
    public IActionResult CancelOrder(string id)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId)) return Unauthorized("User not authenticated.");
        if (!int.TryParse(userId, out var parsedUserId)) return BadRequest("Invalid user ID format.");
        var orderId = int.TryParse(id, out var parsedId) ? parsedId : 0;
        var order = context.Orders.FirstOrDefault(o => o.Id == orderId && o.CustomerId == parsedUserId);
        if (order == null) return NotFound();
        if (order.DispatchStatus == DispatchStatus.Dispatched || order.DispatchStatus == DispatchStatus.Delivered)
            return BadRequest("Order cannot be cancelled after dispatch.");
        order.DispatchStatus = DispatchStatus.Cancelled;
        context.SaveChanges();
        return Ok(order);
    }

    // Admin: Filter orders by date and delivery type
    [HttpGet]
    [Authorize(Policy = "Admin")]
    public IActionResult GetOrders([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string? deliveryType)
    {
        var query = context.Orders.AsQueryable();
        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value);
        if (!string.IsNullOrEmpty(deliveryType))
            query = query.Where(o => o.DeliveryType.ToString().Equals(deliveryType, StringComparison.CurrentCultureIgnoreCase));
        var orders = query.Include(o => o.OrderItems).ToList();
        return Ok(orders);
    }

    // Employee/Admin: Update order details
    [HttpPut("{id}")]
    [Authorize(Policy = "EmployeeOrAdmin")]
    public IActionResult UpdateOrder(string id, [FromBody] Order updated)
    {
        if (updated == null || string.IsNullOrEmpty(id)) return BadRequest("Invalid order data.");

        if (!int.TryParse(id, out var orderId)) return BadRequest("Invalid order ID format.");
        var order = context.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.Id == orderId);
        if (order == null) return NotFound();
        order.DeliveryAddress = updated.DeliveryAddress;
        order.DeliveryType = updated.DeliveryType;
        order.DispatchStatus = updated.DispatchStatus;
        order.UpdatedAt = DateTime.UtcNow;
        // Optionally update order items, etc.
        context.SaveChanges();
        return Ok(order);
    }

    // Employee: Update delivery status/report
    [HttpPut("{id}/delivery")]
    [Authorize(Policy = "Employee")]
    public IActionResult UpdateDeliveryStatus(string id, [FromBody] string newStatus)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(newStatus)) return BadRequest("Invalid order ID or status.");
        if (!int.TryParse(id, out var orderId)) return BadRequest("Invalid order ID format.");
        var order = context.Orders.FirstOrDefault(o => o.Id == orderId);
        if (order == null) return NotFound();
        if (!Enum.TryParse<DispatchStatus>(newStatus, true, out var parsedStatus))
            return BadRequest("Invalid dispatch status.");
        order.DispatchStatus = parsedStatus;
        order.UpdatedAt = DateTime.UtcNow;
        context.SaveChanges();
        return Ok(order);
    }
}