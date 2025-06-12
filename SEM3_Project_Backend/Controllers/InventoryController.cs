using Microsoft.AspNetCore.Mvc;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.Model;

[Route("api/[controller]")]
[ApiController]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public InventoryController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{productId}")]
    public IActionResult GetInventory(string productId)
    {
        var item = _context.InventoryItems.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return NotFound();
        return Ok(new { item.ProductId, item.Quantity });
    }

    [HttpPost("{productId}/add")]
    public IActionResult AddStock(string productId, int quantity)
    {
        var item = _context.InventoryItems.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
        {
            item = new InventoryItem
            {
                ProductId = productId,
                Quantity = quantity,
                UpdatedAt = DateTime.UtcNow
            };
            _context.InventoryItems.Add(item);
        }
        else
        {
            item.Quantity += quantity;
            item.UpdatedAt = DateTime.UtcNow;
        }
        _context.SaveChanges();
        return Ok(item);
    }

    [HttpPut("{productId}/update")]
    public IActionResult UpdateStock(string productId, int quantity)
    {
        var item = _context.InventoryItems.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return NotFound();
        item.Quantity = quantity;
        item.UpdatedAt = DateTime.UtcNow;
        _context.SaveChanges();
        return Ok(item);
    }
}
