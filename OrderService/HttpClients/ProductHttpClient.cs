using OrderService.Models;
using System.Text.Json;

namespace OrderService.HttpClients
{
    public class ProductHttpClient
    {
        private readonly HttpClient _httpClient;

        public ProductHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ProductInfo?> GetProductAsync(int productId)
        {
            var requestUrl = $"api/product/{productId}";

            var response = await _httpClient.GetAsync(requestUrl);

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ProductInfo>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
