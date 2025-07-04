﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.Model;
using SEM3_Project_Backend.Service;

namespace SEM3_Project_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
//TODO: add auth to endpoints if needed, re-check roles and policies
public class PaymentController(AppDbContext context, PaypalService paypalService) : ControllerBase
{
    [HttpPost("start")]  // Start PayPal payment flow, add a Pending payment & return redirect URL
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> StartPayment([FromBody] StartPaymentRequest dto)
    {
        var order = await context.Orders.FindAsync(dto.OrderId);
        if (order is not { DispatchStatus: DispatchStatus.Pending })
            return BadRequest("Invalid or already processed order.");

        var payment = new Payment
        {
            OrderId = order.Id,
            PaymentType = PaymentType.PayPal,
            PaymentStatus = PaymentStatus.Pending,
            Amount = dto.Amount,
            PaymentDate = DateTime.UtcNow
        };

        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        // Pass the URLs from the DTO
        var payUrl = await paypalService.GenerateUrl(order, payment, dto.ReturnUrl, dto.CancelUrl);
        return Ok(new { payUrl });
    }

    [HttpPost("paypal/callback")] // Capture payment after return from PayPal
    [AllowAnonymous]
    public async Task<IActionResult> PayPalCallback([FromBody] PayPalCallbackRequest request)
    {
        return await paypalService.VerifyCallback(request);
    }

    [HttpGet("status/{orderId:int}")] // Get payment status for UI with orderId
    [AllowAnonymous]
    public async Task<IActionResult> GetPaymentStatus(int orderId)
    {
        var payment = await context.Payments
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.PaymentDate)
            .FirstOrDefaultAsync();

        if (payment == null)
            return NotFound();

        return Ok(new
        {
            payment.OrderId,
            payment.PaymentStatus,
            payment.PaymentType,
            payment.TransactionId
        });
    }

    [HttpPut("{paymentId:int}/status")] // Manually update status (admin)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int paymentId, [FromBody] string status)
    {
        var payment = await context.Payments.FindAsync(paymentId);
        if (payment == null) return NotFound();

        if (!Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
            return BadRequest("Invalid status.");

        payment.PaymentStatus = parsedStatus;
        await context.SaveChangesAsync();
        return Ok(payment);
    }

    [HttpDelete("{paymentId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePayment(int paymentId)
    {
        var payment = await context.Payments.FindAsync(paymentId);
        if (payment == null) return NotFound();
        // Prevent deletion if payment is completed/cleared
        if (payment.PaymentStatus == PaymentStatus.Cleared)
            return BadRequest("Cannot delete a completed payment.");
        // Prevent deletion if referenced in order (order must not exist or be deleted first)
        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
        if (order != null)
            return BadRequest("Cannot delete payment: referenced by order.");
        context.Payments.Remove(payment);
        await context.SaveChangesAsync();
        return Ok();
    }
}

public class StartPaymentRequest
{
    public int OrderId { get; set; }
    public float Amount { get; set; }
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class PayPalCallbackRequest
{
    public required string Token { get; set; }
}
