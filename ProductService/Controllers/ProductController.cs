using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Kafka;
using ProductService.Models;
using System.Text.Json;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly KafkaProducer _kafka;
        private readonly IDistributedCache _cache;
        private const string AllProductsCacheKey = "all-products";
        public ProductController(AppDbContext db, KafkaProducer kafka, IDistributedCache cache)
        {
            _db = db;
            _kafka = kafka;
            _cache = cache;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            await _cache.RemoveAsync(AllProductsCacheKey);
            await _kafka.PublishAsync("product-created", new
            {
                ProductId = product.Id,
                product.Name,
                product.Price,
                CreatedAt = DateTime.UtcNow
            });
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product is null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cached = await _cache.GetStringAsync(AllProductsCacheKey);
            if (cached is not null)
            {
                var cachedProducts = JsonSerializer.Deserialize<List<Product>>(cached);
                return Ok(cachedProducts);
            }
            var products = await _db.Products.ToListAsync();
            await _cache.SetStringAsync(AllProductsCacheKey, JsonSerializer.Serialize(products),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            return Ok(products);
        }
    }
}
