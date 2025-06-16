using Microsoft.AspNetCore.Mvc;
using SEM3_Project_Backend.Model;

namespace SEM3_Project_Backend.Interfaces;

public interface IPaymentProviderService
{
    Task<string> GenerateUrl(Order order, Payment payment);
    Task<IActionResult> VerifyCallback(object request);
}
