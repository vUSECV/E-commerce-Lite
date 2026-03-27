using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.DTOs;
using PaymentService.Models;

namespace PaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PaymentController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("process")]
        public async Task<IActionResult> Process(ProcessPaymentDto dto)
        {
            var payment = new Payment
            {
                OrderId = dto.OrderId,
                Amount = dto.Amount,
                Status = PaymentStatus.Pending
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            var random = new Random();
            bool isSuccess = false;
            if (random.NextDouble() < 0.90)
            {
                isSuccess = true;
            }

            payment.Status = isSuccess ? PaymentStatus.Success : PaymentStatus.Failed;
            payment.ProcessedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            if (isSuccess)
            {
                return Ok(new { success = true, paymentId = payment.Id, status = payment.Status.ToString() });
            }

            return Ok(new { success = false, paymentId = payment.Id, status = payment.Status.ToString() });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var payment = await _db.Payments.FindAsync(id);
            if (payment is null)
            {
                return NotFound();
            }
            return Ok(payment);
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
            var payments = await _db.Payments
                .Where(p => p.OrderId == orderId)
                .ToListAsync();
            return Ok(payments);
        }
    }
}
