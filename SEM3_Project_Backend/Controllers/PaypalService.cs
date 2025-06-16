using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SEM3_Project_Backend.Interfaces;
using SEM3_Project_Backend.Model;

namespace SEM3_Project_Backend.Controllers;

public class PaypalService : IPaymentProviderService
{
    private readonly string clientId = Environment.GetEnvironmentVariable("PAYPAL_CLIENT_ID") ?? string.Empty;
    private readonly string clientSecret = Environment.GetEnvironmentVariable("PAYPAL_CLIENT_SECRET") ?? string.Empty;
    
    private async Task<string> GetPayPalAccessToken()
    {
        try
        {
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

            var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await http.PostAsync("https://api-m.sandbox.paypal.com/v1/oauth2/token", body);
            var json = await response.Content.ReadAsStringAsync();
            dynamic token = JsonConvert.DeserializeObject(json) ?? null;

            if (token != null) return token.access_token;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error getting access token:" + ex.Message);
        }
        return string.Empty;
    }

    
    public async Task<string> GenerateUrl(Order order, Payment payment)
    {
        try
        {
            var accessToken = await GetPayPalAccessToken();

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var returnUrl = $"https://yourapp.com/payment-status/{order.GetDisplayId()}";
            var cancelUrl = $"https://yourapp.com/payment-status/{order.GetDisplayId()}?cancel=true";

            var orderPayload = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        reference_id = $"ORDER-{order.GetDisplayId()}",
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

            var content = new StringContent(JsonConvert.SerializeObject(orderPayload), Encoding.UTF8,
                "application/json");
            var response = await http.PostAsync("https://api-m.sandbox.paypal.com/v2/checkout/orders", content);
            var json = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(json);

            string approvalUrl = result?.links?.First(response => response.rel == "approve").href;
            return approvalUrl;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error generating url:" + ex.Message);
        }
    }

    [HttpPost("paypal/capture")]
    public Task<IActionResult> VerifyCallback(object request)
    {
        throw new NotImplementedException();
    }
}