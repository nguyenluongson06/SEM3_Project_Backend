using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.Model;
using SEM3_Project_Backend.DTOs;
using System.Security.Claims;

namespace SEM3_Project_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReturnOrReplacementController(AppDbContext context) : ControllerBase
{
    private int? GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        return int.TryParse(userIdStr, out var id) ? id : null;
    }

    // Helper to get current user's email
    private string? GetCurrentUserEmail()
    {
        return User.Identity?.Name;
    }

    [HttpPost]
    [Authorize(Roles = "Customer")] //only customer can create return|replacement
    public async Task<IActionResult> CreateReturn([FromBody] ReturnOrReplacementDTO dto)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var entity = new ReturnOrReplacement
        {
            OrderId = dto.OrderId,
            ProductId = dto.ProductId,
            RequestType = Enum.TryParse<UserRequestType>(dto.RequestType, out var type) ? type : UserRequestType.Return,
            ApprovalStatus = UserRequestApprovalStatus.Pending,
            RequestDate = DateTime.UtcNow
        };

        context.ReturnOrReplacements.Add(entity);
        await context.SaveChangesAsync();

        dto.Id = entity.Id;
        dto.ApprovalStatus = entity.ApprovalStatus.ToString();
        dto.RequestDate = entity.RequestDate;
        return Ok(dto);
    }

    [HttpGet] 
    [Authorize(Roles = "Admin")] //admin only, get list of returns|replacements
    public IActionResult GetReturnOrReplacement()
    {
        var returns = context.ReturnOrReplacements
            .Select(r => new ReturnOrReplacementDTO
            {
                Id = r.Id,
                OrderId = r.OrderId,
                ProductId = r.ProductId,
                RequestType = r.RequestType.ToString(),
                ApprovalStatus = r.ApprovalStatus.ToString(),
                RequestDate = r.RequestDate
            })
            .ToList();

        return Ok(returns);
    }

    // GET: api/returnorreplacement/my
    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public IActionResult GetMyReturnOrReplacement()
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var customer = context.Customers.FirstOrDefault(c => c.Email == email);
        if (customer == null) return Unauthorized();

        // Join ReturnOrReplacements with Orders to ensure ownership
        var myRequests = (from r in context.ReturnOrReplacements
                         join o in context.Orders on r.OrderId equals o.Id
                         where o.CustomerId == customer.Id
                         select new ReturnOrReplacementDTO
                         {
                             Id = r.Id,
                             OrderId = r.OrderId,
                             ProductId = r.ProductId,
                             RequestType = r.RequestType.ToString(),
                             ApprovalStatus = r.ApprovalStatus.ToString(),
                             RequestDate = r.RequestDate
                         }).ToList();

        return Ok(myRequests);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var request = await context.ReturnOrReplacements.FindAsync(id);
        if (request == null) return NotFound();

        if (!Enum.TryParse<UserRequestApprovalStatus>(status, true, out var parsedStatus))
            return BadRequest("Invalid status.");

        request.ApprovalStatus = parsedStatus;
        await context.SaveChangesAsync();

        var dto = new ReturnOrReplacementDTO
        {
            Id = request.Id,
            OrderId = request.OrderId,
            ProductId = request.ProductId,
            RequestType = request.RequestType.ToString(),
            ApprovalStatus = request.ApprovalStatus.ToString(),
            RequestDate = request.RequestDate
        };

        return Ok(dto);
    }
}
