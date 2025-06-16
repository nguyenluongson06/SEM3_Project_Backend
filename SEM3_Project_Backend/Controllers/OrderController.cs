using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.DTOs;
using SEM3_Project_Backend.Model;

namespace SEM3_Project_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
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
}