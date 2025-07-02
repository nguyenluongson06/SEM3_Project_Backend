using Microsoft.AspNetCore.Mvc;
using SEM3_Project_Backend.Model;

namespace SEM3_Project_Backend.Interfaces;

public interface IPaymentProviderService
{
    Task<string> GenerateUrl(Order order, Payment payment, string? returnUrl = null, string? cancelUrl = null);
    Task<IActionResult> VerifyCallback(object request);
}
