using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.DTOs;
using SEM3_Project_Backend.Model;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace SEM3_Project_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FeedbackController(AppDbContext context) : ControllerBase
{
    private int? GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        return int.TryParse(userIdStr, out var id) ? id : null;
    }

    // Only allow feedback from registered users who have purchased the product
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public IActionResult SubmitFeedback([FromBody] FeedbackRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var hasOrdered = context.Orders
            .Any(o => o.CustomerId == userId &&
                      o.OrderItems!.Any(oi => oi.ProductId == request.ProductId));

        if (!hasOrdered) return BadRequest("User must purchase product before submitting feedback");

        var feedback = new Feedback
        {
            CustomerId = userId.Value,
            ProductId = request.ProductId ?? string.Empty,
            Message = request.Message,
            CreatedAt = DateTime.UtcNow
        };

        context.Feedbacks.Add(feedback);
        context.SaveChanges();

        return Ok();
    }

    // Admin only: get all feedbacks, with filtering and pagination
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult GetFeedbacks(
        [FromQuery] string? productId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = context.Feedbacks
            .Include(f => f.Customer)
            .AsQueryable();

        if (!string.IsNullOrEmpty(productId))
            query = query.Where(f => f.ProductId == productId);
        if (fromDate.HasValue)
            query = query.Where(f => f.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(f => f.CreatedAt <= toDate.Value);

        var total = query.Count();
        var feedbacks = query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FeedbackDTO
            {
                Id = f.Id,
                CustomerName = f.Customer != null ? f.Customer.Name : string.Empty,
                ProductId = f.ProductId,
                Message = f.Message ?? string.Empty,
                CreatedAt = f.CreatedAt
            })
            .ToList();

        return Ok(new { total, feedbacks });
    }

    // Customer: get their own feedbacks
    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public IActionResult GetMyFeedbacks()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var feedbacks = context.Feedbacks
            .Where(f => f.CustomerId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FeedbackDTO
            {
                Id = f.Id,
                CustomerName = f.Customer != null ? f.Customer.Name : "",
                ProductId = f.ProductId,
                Message = f.Message ?? string.Empty,
                CreatedAt = f.CreatedAt
            })
            .ToList();

        return Ok(feedbacks);
    }

    // Public: get all feedbacks for a single product
    [HttpGet("/api/product/{productId}/feedbacks")]
    [AllowAnonymous]
    public IActionResult GetFeedbacksForProduct(string productId)
    {
        var feedbacks = context.Feedbacks
            .Include(f => f.Customer)
            .Where(f => f.ProductId == productId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FeedbackDTO
            {
                Id = f.Id,
                CustomerName = f.Customer != null ? f.Customer.Name : string.Empty,
                ProductId = f.ProductId,
                Message = f.Message ?? string.Empty,
                CreatedAt = f.CreatedAt
            })
            .ToList();

        return Ok(new { feedbacks });
    }
}