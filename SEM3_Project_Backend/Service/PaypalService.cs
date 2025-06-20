using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.Interfaces;
using SEM3_Project_Backend.Model;

namespace SEM3_Project_Backend.Service;

public class PaypalService(IConfiguration config, AppDbContext context) : IPaymentProviderService
{
    private readonly HttpClient _httpClient = new();

    private readonly string _clientId = config["PayPal:ClientId"];
    private readonly string _secret = config["PayPal:Secret"];
    private readonly string _apiBase = "https://api-m.sandbox.paypal.com"; // for sandbox

    public async Task<string> GenerateUrl(Order order, Payment payment)
    {
        var token = await GetAccessToken();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var returnUrl = $"https://yourfrontend.com/payment-status/{order.GetDisplayId()}";
        var cancelUrl = $"https://yourfrontend.com/payment-status/{order.GetDisplayId()}?cancel=true";

        var payload = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = order.GetDisplayId(),
                    amount = new
                    {
                        currency_code = "USD",
                        value = payment.Amount.ToString("0.00")
                    }
                }
            },
            application_context = new
            {
                return_url = returnUrl,
                cancel_url = cancelUrl
            }
        };

        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_apiBase}/v2/checkout/orders", content);
        var json = await response.Content.ReadAsStringAsync();
        var res = JObject.Parse(json);
        string approvalUrl = res["links"]
            .First(l => l["rel"]?.ToString() == "approve")["href"]?.ToString();
        return approvalUrl ?? string.Empty; 
    }

    public async Task<IActionResult> VerifyCallback(object request)
    {
        dynamic req = request;
        string paypalToken = req?.token;

        if (string.IsNullOrEmpty(paypalToken)) return new BadRequestObjectResult("Missing token");

        var accessToken = await GetAccessToken();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.PostAsync($"{_apiBase}/v2/checkout/orders/{paypalToken}/capture", null);
        var json = await response.Content.ReadAsStringAsync();
        dynamic captureResult = JsonConvert.DeserializeObject(json);

        if (captureResult.status == "COMPLETED")
        {
            string displayId = captureResult.purchase_units[0].reference_id;
            int orderId = int.Parse(displayId);
            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return new NotFoundObjectResult("Order not found");

            var payment = await context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
            if (payment == null) return new NotFoundObjectResult("Payment not found");

            payment.PaymentStatus = PaymentStatus.Cleared;
            payment.TransactionId = captureResult.id;
            await context.SaveChangesAsync();

            return new OkObjectResult(new { success = true });
        }

        return new BadRequestObjectResult("Payment not completed");
    }

    private async Task<string> GetAccessToken()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_secret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var body = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await _httpClient.PostAsync($"{_apiBase}/v1/oauth2/token", body);
        var json = await response.Content.ReadAsStringAsync();
        dynamic token = JsonConvert.DeserializeObject(json);

        return token?.access_token ?? string.Empty;
    }
}
