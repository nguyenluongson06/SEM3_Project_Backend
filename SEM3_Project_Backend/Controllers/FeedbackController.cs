using Microsoft.AspNetCore.Mvc;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.DTOs;
using SEM3_Project_Backend.Model;

[Route("api/[controller]")]
[ApiController]
public class FeedbackController : ControllerBase
{
    private readonly AppDbContext _context;

    public FeedbackController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult SubmitFeedback(FeedbackRequest request)
    {
        var hasOrdered = _context.Orders
            .Any(o => o.CustomerId == request.CustomerId &&
                      o.OrderItems!.Any(oi => oi.ProductId == request.ProductId));

        if (!hasOrdered) return BadRequest("User must purchase product before submitting feedback");

        var feedback = new Feedback
        {
            CustomerId = request.CustomerId,
            ProductId = request.ProductId,
            Message = request.Message,
            Rating = request.Rating,
            IsVisible = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Feedbacks.Add(feedback);
        _context.SaveChanges();

        return Ok();
    }

    [HttpGet]
    public IActionResult GetFeedbacks()
    {
        var feedbacks = _context.Feedbacks
            .Where(f => f.IsVisible)
            .Select(f => new {
                f.ProductId,
                f.Rating,
                f.Message,
                f.CreatedAt
            }).ToList();

        return Ok(feedbacks);
    }
}
