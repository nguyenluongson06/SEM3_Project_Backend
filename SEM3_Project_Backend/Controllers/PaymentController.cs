using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.Model;

namespace SEM3_Project_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController(AppDbContext context) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Customer")] // customers must be logged in
    public async Task<IActionResult> CreatePayment([FromBody] Payment payment)
    {
        /* TODO: Validate ownership or order status, then call the payment provider; create payment as pending, then if payment is cleared, change status to cleared, otherwise change to failed; create new payment & call back the payment provider each time new payment is requested by user
        */
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        return Ok(payment);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")] //only admin can change payment status
    public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] string status)
    {
        var payment = await context.Payments.FindAsync(id);
        if (payment == null) return NotFound();

        payment.PaymentStatus = Enum.Parse<PaymentStatus>(status);
        await context.SaveChangesAsync();
        return Ok(payment);
    }
}
