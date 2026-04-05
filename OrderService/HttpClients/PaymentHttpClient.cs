using OrderService.Models;
using System.Text;
using System.Text.Json;

namespace OrderService.HttpClients
{
    public class PaymentHttpClient
    {
        private readonly HttpClient _httpClient;

        public PaymentHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PaymentResult?> ProcessPaymentAsync(int orderId, decimal amount)
        {
            var payload = new { OrderId = orderId, Amount = amount };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/payment/process", content);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PaymentResult>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
