using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.Model;

namespace SEM3_Project_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InventoryController(AppDbContext context) : ControllerBase
{
    [HttpGet("{productId}")]
    public IActionResult GetInventory(string productId)
    {
        var item = context.InventoryItems.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return NotFound();
        return Ok(new { item.ProductId, item.Quantity });
    }

    [HttpPost("{productId}/add")]
    [Authorize(Policy = "EmployeeOrAdmin")]
    public IActionResult AddStock(string productId, int quantity)
    {
        var item = context.InventoryItems.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
        {
            item = new InventoryItem
            {
                ProductId = productId,
                Quantity = quantity,
                UpdatedAt = DateTime.UtcNow
            };
            context.InventoryItems.Add(item);
        }
        else
        {
            item.Quantity += quantity;
            item.UpdatedAt = DateTime.UtcNow;
        }
        context.SaveChanges();
        return Ok(item);
    }

    [HttpPut("{productId}/update")]
    [Authorize(Policy = "EmployeeOrAdmin")]
    public IActionResult UpdateStock(string productId, int quantity)
    {
        var item = context.InventoryItems.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return NotFound();
        item.Quantity = quantity;
        item.UpdatedAt = DateTime.UtcNow;
        context.SaveChanges();
        return Ok(item);
    }
}