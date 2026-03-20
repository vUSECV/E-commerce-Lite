using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.HttpClients;
using OrderService.Kafka;
using OrderService.Models;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ProductHttpClient _productClient;
        private readonly InventoryHttpClient _inventoryClient;
        private readonly PaymentHttpClient _paymentClient;
        private readonly ILogger<OrderController> _logger;
        private readonly KafkaProducer _kafka;

        public OrderController(
            AppDbContext db,
            ProductHttpClient productClient,
            InventoryHttpClient inventoryClient,
            PaymentHttpClient paymentClient,
            ILogger<OrderController> logger,
            KafkaProducer kafka)
        {
            _db = db;
            _productClient = productClient;
            _inventoryClient = inventoryClient;
            _paymentClient = paymentClient;
            _logger = logger;
            _kafka = kafka;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            var product = await _productClient.GetProductAsync(dto.ProductId);
            if (product is null)
            {
                _logger.LogWarning("Продукт {ProductId} не найден", dto.ProductId);
                return BadRequest(new { message = $"Продукт {dto.ProductId} не найден" });
            }

            var totalAmount = product.Price * dto.Quantity;

            var order = new Order
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                TotalAmount = totalAmount,
                Status = OrderStatus.Pending
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Заказ {OrderId} создан, продукт зарезервирован...", order.Id);

            var reserved = await _inventoryClient.ReserveStockAsync(dto.ProductId, dto.Quantity);
            if (!reserved)
            {
                order.Status = OrderStatus.Cancelled;
                await _db.SaveChangesAsync();
                _logger.LogWarning("Заказ {OrderId} отменен: недостаточно товара на складе", order.Id);
                return BadRequest(new { message = "недостаточно товара на складе", orderId = order.Id, status = order.Status.ToString() });
            }

            order.Status = OrderStatus.InventoryReserved;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Заказ {OrderId} зарезервирован товар, процесс оплаты...", order.Id);

            order.Status = OrderStatus.PaymentProcessing;
            await _db.SaveChangesAsync();

            var paymentResult = await _paymentClient.ProcessPaymentAsync(order.Id, totalAmount);

            if (paymentResult is null || !paymentResult.Success)
            {
                await _inventoryClient.ReleaseStockAsync(dto.ProductId, dto.Quantity);
                order.Status = OrderStatus.Failed;
                await _db.SaveChangesAsync();
                _logger.LogWarning("Заказ {OrderId} ошибка: платеж не удался", order.Id);
                return Ok(new
                {
                    message = "Ошибка платежа, инвентарь выпущен",
                    orderId = order.Id,
                    status = order.Status.ToString()
                });
            }

            await _inventoryClient.ConfirmStockAsync(dto.ProductId, dto.Quantity);
            order.Status = OrderStatus.Completed;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Заказ {OrderId} удался", order.Id);
            await _kafka.PublishAsync("order-created", new
            {
                OrderId = order.Id,
                order.ProductId,
                ProductName = product.Name,
                order.Quantity,
                order.TotalAmount,
                Status = order.Status.ToString(),
                CreatedAt = DateTime.UtcNow
            });
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, new
            {
                message = "Заказ выполнен",
                orderId = order.Id,
                status = order.Status.ToString(),
                totalAmount,
                paymentId = paymentResult.PaymentId
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order is null)
            {
                return NotFound();
            }
            return Ok(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _db.Orders.ToListAsync());
        }
    }
}
