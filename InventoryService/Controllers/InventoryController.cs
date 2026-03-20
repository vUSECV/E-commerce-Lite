using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryService.Data;
using InventoryService.DTOs;
using InventoryService.Models;

namespace InventoryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _db;

        public InventoryController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("stock")]
        public async Task<IActionResult> AddStock([FromBody] AddStockDto dto)
        {
            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(x => x.ProductId == dto.ProductId);

            if (item is null)
            {
                item = new InvenoryItem
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                };
                _db.InventoryItems.Add(item);
            }
            else
            {
                item.Quantity += dto.Quantity;
            }

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPost("reserve")]
        public async Task<IActionResult> Reserve([FromBody] ReserveStockDto dto)
        {
            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(x => x.ProductId == dto.ProductId);

            if (item is null || item.AvailableQuantity < dto.Quantity)
            {
                return BadRequest(new { success = false, message = "Недостаточно товара на складе" });
            }

            item.ReservedQuantity += dto.Quantity;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Зарезервирован" });
        }

        [HttpPost("release")]
        public async Task<IActionResult> Release([FromBody] ReleaseStockDto dto)
        {
            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(x => x.ProductId == dto.ProductId);

            if (item is null)
            {
                return NotFound();
            }

            item.ReservedQuantity = Math.Max(0, item.ReservedQuantity - dto.Quantity);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Резервирование запасов разрешено" });
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] ConfirmStockDto dto)
        {
            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(x => x.ProductId == dto.ProductId);

            if (item is null)
            {
                return NotFound();
            }

            item.Quantity -= dto.Quantity;
            item.ReservedQuantity = Math.Max(0, item.ReservedQuantity - dto.Quantity);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Товар подтвержден и вычтен" });
        }

        [HttpGet("{productId}")]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(x => x.ProductId == productId);

            if (item is null)
            {
                return NotFound();
            }
            return Ok(item);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _db.InventoryItems.ToListAsync());
        }
    }
}
