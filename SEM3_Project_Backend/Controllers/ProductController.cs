using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.Model;

namespace SEM3_Project_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController(AppDbContext context) : ControllerBase
{
    // Public: List all products
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetProducts()
    {
        var products = context.Products
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.WarrantyPeriod,
                Category = p.Category != null ? p.Category.Name : null,
                Inventory = p.InventoryItem != null ? p.InventoryItem.Quantity : 0
            })
            .ToList();
        return Ok(products);
    }

    // Public: Get product by id
    [HttpGet("{id}")]
    [AllowAnonymous]
    public IActionResult GetProduct(string id)
    {
        var product = context.Products
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.WarrantyPeriod,
                Category = p.Category != null ? p.Category.Name : null,
                Inventory = p.InventoryItem != null ? p.InventoryItem.Quantity : 0
            })
            .FirstOrDefault();
        return product == null ? NotFound() : Ok(product);
    }

    // Admin/Employee: Create new product
    [HttpPost]
    [Authorize(Policy = "EmployeeOrAdmin")]
    public IActionResult CreateProduct(Product product)
    {
        if (context.Products.Any(p => p.Id == product.Id))
            return BadRequest("Product ID already exists.");

        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        context.Products.Add(product);
        context.SaveChanges();
        return Ok(product);
    }

    // Admin/Employee: Update product
    [HttpPut("{id}")]
    [Authorize(Policy = "EmployeeOrAdmin")]
    public IActionResult UpdateProduct(string id, Product updated)
    {
        var product = context.Products.FirstOrDefault(p => p.Id == id);
        if (product == null) return NotFound();

        product.Name = updated.Name;
        product.Description = updated.Description;
        product.Price = updated.Price;
        product.CategoryId = updated.CategoryId;
        product.WarrantyPeriod = updated.WarrantyPeriod;
        product.UpdatedAt = DateTime.UtcNow;
        context.SaveChanges();
        return Ok(product);
    }

    // Admin/Employee: Delete product
    [HttpDelete("{id}")]
    [Authorize(Policy = "EmployeeOrAdmin")]
    public IActionResult DeleteProduct(string id)
    {
        var product = context.Products.FirstOrDefault(p => p.Id == id);
        if (product == null) return NotFound();
        context.Products.Remove(product);
        context.SaveChanges();
        return Ok();
    }
}