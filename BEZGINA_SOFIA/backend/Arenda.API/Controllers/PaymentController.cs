using Arenda.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Claims;

namespace Arenda.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly DatabaseService _dbService;

        public PaymentController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        // GET: api/payment/methods
        [HttpGet("methods")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            try
            {
                using var conn = new NpgsqlConnection(_dbService.DataAccess.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand("SELECT id, nazvanie FROM sposob_oplaty", conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var methods = new List<object>();
                while (await reader.ReadAsync())
                {
                    methods.Add(new { id = reader.GetInt32(0), name = reader.GetString(1) });
                }

                return Ok(methods);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/payment/statuses
        [HttpGet("statuses")]
        public async Task<IActionResult> GetPaymentStatuses()
        {
            try
            {
                using var conn = new NpgsqlConnection(_dbService.DataAccess.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand("SELECT id, nazvanie_statusa FROM status_predoplaty", conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var statuses = new List<object>();
                while (await reader.ReadAsync())
                {
                    statuses.Add(new { id = reader.GetInt32(0), name = reader.GetString(1) });
                }

                return Ok(statuses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/payment/process (шуточная оплата)
        [Authorize]
        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized();

                var userId = int.Parse(userIdClaim.Value);

                Console.WriteLine($"=== ШУТОЧНАЯ ОПЛАТА ===");
                Console.WriteLine($"Пользователь: {userId}");
                Console.WriteLine($"Бронирование: {request.BookingId}");
                Console.WriteLine($"Сумма: {request.Amount} ₽");
                Console.WriteLine($"Способ оплаты: {request.PaymentMethodId}");

                // Шуточная "оплата" - просто возвращаем успех
                return Ok(new
                {
                    success = true,
                    message = $"✅ Оплата прошла успешно! Списано {request.Amount} ₽ (тестовый режим)",
                    transactionId = $"TST-{DateTime.Now.Ticks}"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class PaymentRequest
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public int PaymentMethodId { get; set; }
    }
}
