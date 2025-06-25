using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.DTOs;
using SEM3_Project_Backend.Model;

namespace SEM3_Project_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController(AppDbContext context) : ControllerBase
{
    // Get all categories (public)
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetCategories()
    {
        var categories = context.Categories
            .Select(c => new CategoryDTO
            {
                Id = c.Id,
                Name = c.Name ?? "",
                ImageUrl = c.ImageUrl,
                CreatedAt = c.CreatedAt,
                ModifiedAt = c.ModifiedAt
            })
            .ToList();
        return Ok(categories);
    }

    // Get category by id (public)
    [HttpGet("{id}")]
    [AllowAnonymous]
    public IActionResult GetCategory(int id)
    {
        var cat = context.Categories
            .Where(c => c.Id == id)
            .Select(c => new CategoryDTO
            {
                Id = c.Id,
                Name = c.Name ?? "",
                ImageUrl = c.ImageUrl,
                CreatedAt = c.CreatedAt,
                ModifiedAt = c.ModifiedAt
            })
            .FirstOrDefault();
        return cat == null ? NotFound() : Ok(cat);
    }

    // Add new category (employee/admin)
    [HttpPost("add")]
    [Authorize(Policy = "EmployeeOrAdmin")]
    public IActionResult AddCategory([FromBody] CategoryDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Category name is required.");

        if (context.Categories.Any(c => c.Name != null && c.Name.ToLower() == dto.Name.ToLower()))
            return BadRequest("Category already exists.");

        var cat = new Category
        {
            Name = dto.Name.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl)
                ? "https://d2opxh93rbxzdn.cloudfront.net/original/2X/4/40cfa8ca1f24ac29cfebcb1460b5cafb213b6105.png"
                : dto.ImageUrl,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        context.Categories.Add(cat);
        context.SaveChanges();

        dto.Id = cat.Id;
        dto.ImageUrl = cat.ImageUrl;
        dto.CreatedAt = cat.CreatedAt;
        dto.ModifiedAt = cat.ModifiedAt;
        return Ok(dto);
    }

    // Update category info (employee/admin)
    [HttpPut("{id}")]
    [Authorize(Policy = "EmployeeOrAdmin")]
    public IActionResult UpdateCategory(int id, [FromBody] CategoryDTO dto)
    {
        var cat = context.Categories.FirstOrDefault(c => c.Id == id);
        if (cat == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Name))
            cat.Name = dto.Name.Trim();
        if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
            cat.ImageUrl = dto.ImageUrl;
        cat.ModifiedAt = DateTime.UtcNow;

        context.SaveChanges();
        return Ok(new CategoryDTO
        {
            Id = cat.Id,
            Name = cat.Name ?? "",
            ImageUrl = cat.ImageUrl,
            CreatedAt = cat.CreatedAt,
            ModifiedAt = cat.ModifiedAt
        });
    }

    // Delete category (employee/admin)
    [HttpDelete("{id}")]
    [Authorize(Policy = "EmployeeOrAdmin")]
    public IActionResult DeleteCategory(int id)
    {
        var cat = context.Categories.Include(c => c.Products).FirstOrDefault(c => c.Id == id);
        if (cat == null) return NotFound();
        if (cat.Products != null && cat.Products.Any())
            return BadRequest("Cannot delete category with products.");

        context.Categories.Remove(cat);
        context.SaveChanges();
        return Ok();
    }

    // Get products by category (public)
    [HttpGet("{id}/products")]
    [AllowAnonymous]
    public IActionResult GetProductsByCategory(int id)
    {
        var products = context.Products
            .Where(p => p.CategoryId == id)
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
}