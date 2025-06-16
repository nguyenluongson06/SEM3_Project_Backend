using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.Model;

namespace SEM3_Project_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReturnOrReplacementController(AppDbContext context) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Customer")] //only customer can create feedbacks
    public async Task<IActionResult> CreateReturn([FromBody] ReturnOrReplacement request)
    {
        context.ReturnOrReplacements.Add(request);
        await context.SaveChangesAsync();
        return Ok(request);
    }

    [HttpGet] 
    [Authorize(Roles = "Admin")] //admin only, get list of returns|replacements
    public Task<IActionResult> GetReturnOrReplacement()
    {
        try
        {
            var returns = context.ReturnOrReplacements.ToList();
            return Task.FromResult<IActionResult>(Ok(returns));
        }
        catch (Exception e)
        {
            return Task.FromResult<IActionResult>(StatusCode(200, e.Message));
        }
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var request = await context.ReturnOrReplacements.FindAsync(id);
        if (request == null) return NotFound();

        request.ApprovalStatus = Enum.Parse<UserRequestApprovalStatus>(status);
        await context.SaveChangesAsync();
        return Ok(request);
    }
}
