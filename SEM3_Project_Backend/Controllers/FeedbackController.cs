using Microsoft.AspNetCore.Mvc;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.DTOs;
using SEM3_Project_Backend.Model;

namespace SEM3_Project_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FeedbackController(AppDbContext context) : ControllerBase
{
    //TODO: use DTO instead of model directly
    //TODO: add validation for request, only allow feedback from registered users who have purchased the product
    [HttpPost]
    public IActionResult SubmitFeedback(FeedbackRequest request)
    {
        var hasOrdered = context.Orders
            .Any(o => o.CustomerId == request.CustomerId &&
                      o.OrderItems!.Any(oi => oi.ProductId == request.ProductId));

        if (!hasOrdered) return BadRequest("User must purchase product before submitting feedback");

        var feedback = new Feedback
        {
            CustomerId = request.CustomerId,
            //ProductId = request.ProductId, //feedback khong co productId, tam thoi nhan het toan bo feedback
            Message = request.Message,
            //Rating = request.Rating, tam thoi chua co rating
            //IsVisible = false, khong can, tam thoi chi la feedback luu o backend
            CreatedAt = DateTime.UtcNow
        };

        context.Feedbacks.Add(feedback);
        context.SaveChanges();

        return Ok();
    }

    [HttpGet]
    public IActionResult GetFeedbacks()
    {
        /*
    var feedbacks = _context.Feedbacks
        .Where(f => f.IsVisible)
        .Select(f => new {
            f.ProductId,
            f.Rating,
            f.Message,
            f.CreatedAt
        }).ToList();
    */

        var feedbacks = context.Feedbacks.Select(f => new
        {
            f.Customer!.Name,
            f.Message,
            f.CreatedAt,
        }).ToList();
        
        return Ok(feedbacks);
    }
}