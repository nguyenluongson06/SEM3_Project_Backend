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
    private readonly string _secret = config["PayPal:ClientSecret"];
    private readonly string _apiBase = "https://api-m.sandbox.paypal.com"; // for sandbox

    public async Task<string> GenerateUrl(Order order, Payment payment, string? returnUrl = null, string? cancelUrl = null)
        
    {
        // Log PayPal credentials and environment for debugging
        Console.WriteLine($"[PayPal][GenerateUrl] ClientId: {_clientId}, Secret: {_secret}, ApiBase: {_apiBase}");
        var token = await GetAccessToken();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Use provided URLs or fallback
        returnUrl ??= $"https://yourfrontend.com/payment-status/{order.GetDisplayId()}";
        cancelUrl ??= $"https://yourfrontend.com/payment-status/{order.GetDisplayId()}?cancel=true";

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
        // Log PayPal credentials and environment for debugging
        Console.WriteLine($"[PayPal][VerifyCallback] ClientId: {_clientId}, Secret: {_secret}, ApiBase: {_apiBase}");
        // Accepts PayPalCallbackRequest with property 'Token'
        string? paypalToken = null;
        var debugInfo = new Dictionary<string, object?>();
        // Try to get the Token property via reflection (works for PayPalCallbackRequest and anonymous/dynamic)
        var type = request.GetType();
        debugInfo["requestType"] = type.FullName;
        var prop = type.GetProperty("Token");
        debugInfo["hasTokenProperty"] = prop != null;
        if (prop != null)
            paypalToken = prop.GetValue(request)?.ToString();
        debugInfo["paypalToken"] = paypalToken;

        if (string.IsNullOrEmpty(paypalToken)) {
            Console.WriteLine($"[PayPalCallback][DEBUG] {JsonConvert.SerializeObject(debugInfo)}");
            return new BadRequestObjectResult(new { error = "Missing token", debug = debugInfo });
        }

        var accessToken = await GetAccessToken();
        debugInfo["accessTokenNullOrEmpty"] = string.IsNullOrEmpty(accessToken);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_apiBase}/v2/checkout/orders/{paypalToken}/capture");
        // Send empty JSON object as body, with correct content type
        requestMessage.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(requestMessage);
        debugInfo["paypalApiStatusCode"] = (int)response.StatusCode;
        var json = await response.Content.ReadAsStringAsync();
        debugInfo["paypalApiResponse"] = json;
        if (string.IsNullOrEmpty(json)) {
            Console.WriteLine($"[PayPalCallback][DEBUG] {JsonConvert.SerializeObject(debugInfo)}");
            return new BadRequestObjectResult(new { error = "No response from PayPal", debug = debugInfo });
        }
        dynamic? captureResult = JsonConvert.DeserializeObject(json);
        debugInfo["captureResultNull"] = captureResult == null;
        if (captureResult == null) {
            Console.WriteLine($"[PayPalCallback][DEBUG] {JsonConvert.SerializeObject(debugInfo)}");
            return new BadRequestObjectResult(new { error = "Invalid response from PayPal", debug = debugInfo });
        }

        debugInfo["captureResultStatus"] = captureResult.status != null ? captureResult.status.ToString() : null;
        if (captureResult.status == "COMPLETED")
        {
            string displayId = captureResult.purchase_units[0].reference_id;
            debugInfo["displayId"] = displayId;
            int orderId = int.Parse(displayId);
            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            debugInfo["orderFound"] = order != null;
            if (order == null) {
                Console.WriteLine($"[PayPalCallback][DEBUG] {JsonConvert.SerializeObject(debugInfo)}");
                return new NotFoundObjectResult(new { error = "Order not found", debug = debugInfo });
            }

            var payment = await context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
            debugInfo["paymentFound"] = payment != null;
            if (payment == null) {
                Console.WriteLine($"[PayPalCallback][DEBUG] {JsonConvert.SerializeObject(debugInfo)}");
                return new NotFoundObjectResult(new { error = "Payment not found", debug = debugInfo });
            }

            payment.PaymentStatus = PaymentStatus.Cleared;
            payment.TransactionId = captureResult.id;
            await context.SaveChangesAsync();

            Console.WriteLine($"[PayPalCallback][DEBUG] {JsonConvert.SerializeObject(debugInfo)}");
            return new OkObjectResult(new { success = true, debug = debugInfo });
        }

        Console.WriteLine($"[PayPalCallback][DEBUG] {JsonConvert.SerializeObject(debugInfo)}");
        return new BadRequestObjectResult(new { error = "Payment not completed", debug = debugInfo });
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
        if (string.IsNullOrEmpty(json))
            return string.Empty;
        dynamic? token = JsonConvert.DeserializeObject(json);
        if (token == null)
            return string.Empty;

        return token.access_token ?? string.Empty;
    }
}
