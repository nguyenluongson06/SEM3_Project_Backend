using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.Model;
using SEM3_Project_Backend.DTOs;

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
            .Include(p => p.Category)
            .Include(p => p.InventoryItem)
            .Select(p => new ProductDTO
            {
                Id = p.Id,
                Name = p.Name ?? "",
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                WarrantyPeriod = p.WarrantyPeriod,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null,
                InventoryQuantity = p.InventoryItem != null ? p.InventoryItem.Quantity : 0
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
            .Include(p => p.Category)
            .Include(p => p.InventoryItem)
            .Where(p => p.Id == id)
            .Select(p => new ProductDTO
            {
                Id = p.Id,
                Name = p.Name ?? "",
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                WarrantyPeriod = p.WarrantyPeriod,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null,
                InventoryQuantity = p.InventoryItem != null ? p.InventoryItem.Quantity : 0
            })
            .FirstOrDefault();
        return product == null ? NotFound() : Ok(product);
    }

    // Public: Get products by price range
    [HttpGet("price")]
    [AllowAnonymous]
    public IActionResult GetProductsByPrice([FromQuery] float? min, [FromQuery] float? max)
    {
        var query = context.Products
            .Include(p => p.Category)
            .Include(p => p.InventoryItem)
            .AsQueryable();

        if (min.HasValue)
            query = query.Where(p => p.Price >= min.Value);
        if (max.HasValue)
            query = query.Where(p => p.Price <= max.Value);

        var products = query
            .Select(p => new ProductDTO
            {
                Id = p.Id,
                Name = p.Name ?? "",
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                WarrantyPeriod = p.WarrantyPeriod,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null,
                InventoryQuantity = p.InventoryItem != null ? p.InventoryItem.Quantity : 0
            })
            .ToList();

        return Ok(products);
    }

    // Admin/Employee: Create new product
    [HttpPost]
    [Authorize(Policy = "EmployeeOrAdmin")]
    public IActionResult CreateProduct([FromBody] ProductDTO dto)
    {
        var category = context.Categories.FirstOrDefault(c => c.Id == dto.CategoryId);
        if (category == null)
            return BadRequest("Invalid category.");

        // Generate product ID: 2-char cat + 5-digit number
        var catCode = (category.Name?.Length ?? 0) >= 2 ? category.Name!.Substring(0, 2).ToUpper() : "XX";
        var count = context.Products.Count(p => p.CategoryId == dto.CategoryId) + 1;
        var prodId = $"{catCode}{count:D5}";

        var product = new Product
        {
            Id = prodId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            ImageUrl = dto.ImageUrl,
            CategoryId = dto.CategoryId,
            WarrantyPeriod = dto.WarrantyPeriod,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create inventory item
        product.InventoryItem = new InventoryItem
        {
            ProductId = prodId,
            Quantity = dto.InventoryQuantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        context.SaveChanges();

        // Return the created product as DTO
        dto.Id = product.Id;
        dto.CategoryName = category.Name;
        return Ok(dto);
    }

    // Admin/Employee: Update product
    [HttpPut("{id}")]
    [Authorize(Policy = "EmployeeOrAdmin")]
    public IActionResult UpdateProduct(string id, [FromBody] ProductDTO dto)
    {
        var product = context.Products.Include(p => p.InventoryItem).FirstOrDefault(p => p.Id == id);
        if (product == null) return NotFound();

        // Prevent update if product is in any order, return, or feedback
        bool inOrder = context.OrderItems.Any(oi => oi.ProductId == id);
        bool inReturn = context.ReturnOrReplacements.Any(rr => rr.ProductId == id);
        bool inFeedback = context.Feedbacks.Any(fb => fb.ProductId == id);
        bool hasStock = product.InventoryItem != null && product.InventoryItem.Quantity > 0;

        if (inOrder || inReturn || inFeedback || hasStock)
            return BadRequest("Cannot modify product: referenced in order/return/feedback or stock not zero.");

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.ImageUrl = dto.ImageUrl;
        product.CategoryId = dto.CategoryId;
        product.WarrantyPeriod = dto.WarrantyPeriod;
        product.UpdatedAt = DateTime.UtcNow;

        // Update inventory if needed
        if (product.InventoryItem != null)
        {
            product.InventoryItem.Quantity = dto.InventoryQuantity;
            product.InventoryItem.UpdatedAt = DateTime.UtcNow;
        }

        context.SaveChanges();
        return Ok(dto);
    }

    // Admin/Employee: Delete product
    [HttpDelete("{id}")]
    [Authorize(Policy = "EmployeeOrAdmin")]
    public IActionResult DeleteProduct(string id)
    {
        var product = context.Products.Include(p => p.InventoryItem).FirstOrDefault(p => p.Id == id);
        if (product == null) return NotFound();

        bool inOrder = context.OrderItems.Any(oi => oi.ProductId == id);
        bool inReturn = context.ReturnOrReplacements.Any(rr => rr.ProductId == id);
        bool inFeedback = context.Feedbacks.Any(fb => fb.ProductId == id);
        bool hasStock = product.InventoryItem != null && product.InventoryItem.Quantity > 0;

        if (inOrder || inReturn || inFeedback || hasStock)
            return BadRequest("Cannot delete product: referenced in order/return/feedback or stock not zero.");

        context.Products.Remove(product);
        context.SaveChanges();
        return Ok();
    }
}