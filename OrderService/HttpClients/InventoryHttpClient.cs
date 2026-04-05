using System.Text;
using System.Text.Json;

namespace OrderService.HttpClients
{
    public class InventoryHttpClient
    {
        private readonly HttpClient _httpClient;

        public InventoryHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> ReserveStockAsync(int productId, int quantity)
        {
            var payload = new { ProductId = productId, Quantity = quantity };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/inventory/reserve", content);

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<JsonElement>(json);

            return result.GetProperty("success").GetBoolean();
        }

        public async Task<bool> ReleaseStockAsync(int productId, int quantity)
        {
            var payload = new { ProductId = productId, Quantity = quantity };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/inventory/release", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ConfirmStockAsync(int productId, int quantity)
        {
            var payload = new { ProductId = productId, Quantity = quantity };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/inventory/confirm", content);
            return response.IsSuccessStatusCode;
        }
    }
}
