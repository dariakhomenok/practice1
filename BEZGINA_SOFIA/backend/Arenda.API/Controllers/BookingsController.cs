using Arenda.API.Models;
using Arenda.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Claims;

namespace Arenda.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly DatabaseService _dbService;

        public BookingsController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        // ========== ПОЛУЧИТЬ БРОНИРОВАНИЯ АРЕНДОДАТЕЛЯ ==========
        [Authorize]
        [HttpGet("landlord/{userId}")]
        public async Task<IActionResult> GetLandlordBookings(int userId)
        {
            try
            {
                // Проверяем, что запрашивает свои данные
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null)
                    return Unauthorized(new { message = "Не авторизован" });

                var currentUserId = int.Parse(currentUserIdClaim.Value);
                if (currentUserId != userId)
                    return Forbid();

                using var reader = await _dbService.DataAccess.GetLandlordBookingsAsync(userId);
                var bookings = new List<LandlordBookingDto>();

                while (await reader.ReadAsync())
                {
                    bookings.Add(new LandlordBookingDto
                    {
                        Id = reader.GetInt32(0),
                        ApartmentId = reader.GetInt32(1),
                        ApartmentAddress = reader.GetString(2),
                        TenantId = reader.GetInt32(3),
                        TenantName = reader.GetString(4),
                        TenantPhone = reader.GetString(5),
                        CheckIn = reader.GetDateTime(6),      // ← DateTime
                        CheckOut = reader.GetDateTime(7),     // ← DateTime
                        Status = reader.GetString(8),
                        TotalPrice = reader.GetDecimal(9),
                        CreatedAt = reader.GetDateTime(10)    // ← DateTime
                    });
                }

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при получении списка бронирований" });
            }
        }

        // ========== ПОДТВЕРДИТЬ БРОНИРОВАНИЕ ==========
        [Authorize]
        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            try
            {
                // Проверяем, что пользователь имеет право
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Не авторизован" });

                var userId = int.Parse(userIdClaim.Value);

                // Проверяем, что бронирование принадлежит квартире этого арендодателя
                using var checkReader = await _dbService.DataAccess.GetBookingByIdAsync(id);
                if (!await checkReader.ReadAsync())
                    return NotFound(new { message = "Бронирование не найдено" });

                var apartmentId = checkReader.GetInt32(2);
                await checkReader.CloseAsync();

                // Проверяем, что квартира принадлежит этому арендодателю
                using var aptReader = await _dbService.DataAccess.GetApartmentByIdAsync(apartmentId);
                if (!await aptReader.ReadAsync())
                    return NotFound(new { message = "Квартира не найдена" });

                var landlordId = aptReader.GetInt32(1);
                await aptReader.CloseAsync();

                if (landlordId != userId)
                    return Forbid();

                // Подтверждаем бронирование
                var result = await _dbService.DataAccess.ConfirmBookingAsync(id);
                if (result > 0)
                    return Ok(new { message = "Бронирование подтверждено" });
                else
                    return BadRequest(new { message = "Не удалось подтвердить бронирование. Возможно, оно уже обработано" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при подтверждении бронирования" });
            }
        }

        // ========== ОТМЕНИТЬ БРОНИРОВАНИЕ ==========
        [Authorize]
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            try
            {
                // Проверяем авторизацию
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Не авторизован" });

                var userId = int.Parse(userIdClaim.Value);

                // Проверяем, что бронирование принадлежит квартире этого арендодателя
                using var checkReader = await _dbService.DataAccess.GetBookingByIdAsync(id);
                if (!await checkReader.ReadAsync())
                    return NotFound(new { message = "Бронирование не найдено" });

                var apartmentId = checkReader.GetInt32(2);
                await checkReader.CloseAsync();

                using var aptReader = await _dbService.DataAccess.GetApartmentByIdAsync(apartmentId);
                if (!await aptReader.ReadAsync())
                    return NotFound(new { message = "Квартира не найдена" });

                var landlordId = aptReader.GetInt32(1);
                await aptReader.CloseAsync();

                if (landlordId != userId)
                    return Forbid();

                // Отменяем бронирование
                var result = await _dbService.DataAccess.CancelBookingAsync(id);
                if (result > 0)
                    return Ok(new { message = "Бронирование отменено" });
                else
                    return BadRequest(new { message = "Не удалось отменить бронирование. Возможно, оно уже подтверждено или отменено" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при отмене бронирования" });
            }
        }
        // ========== ПОЛУЧИТЬ БРОНИРОВАНИЯ АРЕНДАТОРА ==========
        [Authorize]
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserBookings(int userId)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null)
                    return Unauthorized(new { message = "Не авторизован" });

                var currentUserId = int.Parse(currentUserIdClaim.Value);
                if (currentUserId != userId)
                    return Forbid();

                using var reader = await _dbService.DataAccess.GetUserBookingsAsync(userId);
                var bookings = new List<UserBookingDto>();

                while (await reader.ReadAsync())
                {
                    bookings.Add(new UserBookingDto
                    {
                        Id = reader.GetInt32(0),
                        ApartmentId = reader.GetInt32(1),
                        ApartmentAddress = reader.GetString(2),
                        PricePerDay = reader.GetDecimal(3),
                        CheckIn = reader.GetDateTime(4),      
                        CheckOut = reader.GetDateTime(5),     
                        Status = reader.GetString(6),
                        TotalPrice = reader.GetDecimal(7),
                        CreatedAt = reader.GetDateTime(8)
                    });
                }

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при получении ваших бронирований" });
            }
        }
        // ========== ПРОВЕРИТЬ ДОСТУПНОСТЬ ==========
        [HttpGet("check-availability")]
        public async Task<IActionResult> CheckAvailability([FromQuery] int apartmentId,
                                                            [FromQuery] DateTime checkIn,
                                                            [FromQuery] DateTime checkOut)
        {
            try
            {
                var isAvailable = await _dbService.DataAccess.IsApartmentAvailableAsync(apartmentId, checkIn, checkOut);
                return Ok(new { available = isAvailable });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при проверке доступности квартиры" });
            }
        }
        // ========== СОЗДАТЬ БРОНИРОВАНИЕ ==========
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Не авторизован" });

                var userId = int.Parse(userIdClaim.Value);

                // Проверяем, что даты корректны
                if (request.CheckIn >= request.CheckOut)
                    return BadRequest(new { message = "Дата выезда должна быть позже даты заезда" });

                // Проверяем, что квартира существует
                using var aptReader = await _dbService.DataAccess.GetApartmentByIdAsync(request.ApartmentId);
                if (!await aptReader.ReadAsync())
                    return NotFound(new { message = "Квартира не найдена" });
                await aptReader.CloseAsync();

                // Проверяем доступность
                var isAvailable = await _dbService.DataAccess.IsApartmentAvailableAsync(
                    request.ApartmentId, request.CheckIn, request.CheckOut);

                if (!isAvailable)
                    return BadRequest(new { message = "Квартира уже забронирована на выбранные даты" });

                // Создаём бронирование
                var bookingId = await _dbService.DataAccess.CreateBookingAsync(
                    userId, request.ApartmentId, request.CheckIn, request.CheckOut);

                return Ok(new { id = bookingId, message = "Бронирование создано" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при создании бронирования. Проверьте правильность введённых данных" });
            }
        }

        // ========== ПОЛУЧИТЬ ЗАВЕРШЕННЫЕ БРОНИРОВАНИЯ АРЕНДАТОРА ==========
        [Authorize]
        [HttpGet("my-completed")]
        public async Task<IActionResult> GetMyCompletedBookings([FromQuery] int? apartmentId = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Не авторизован" });

                var userId = int.Parse(userIdClaim.Value);

                // Формируем запрос
                string sql = @"
                SELECT b.id, b.kartochka_kvartiry_id, b.data_zayezda, b.data_vyyezda,
                       kk.adres_kvartiry, kk.tsena
                FROM bronirovaniye b
                JOIN kartochka_kvartiry kk ON kk.id = b.kartochka_kvartiry_id
                WHERE b.arendator_polzovatel_id = @userId
                  AND b.data_vyyezda < NOW()
                  AND NOT EXISTS(SELECT 1 FROM otzivy o WHERE o.bronirovaniye_id = b.id)";

                if (apartmentId.HasValue && apartmentId.Value > 0)
                    sql += " AND b.kartochka_kvartiry_id = @apartmentId";

                sql += " ORDER BY b.data_vyyezda DESC";

                using var conn = new NpgsqlConnection(_dbService.DataAccess.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                if (apartmentId.HasValue && apartmentId.Value > 0)
                    cmd.Parameters.AddWithValue("@apartmentId", apartmentId.Value);

                var bookings = new List<CompletedBookingDto>();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    bookings.Add(new CompletedBookingDto
                    {
                        Id = reader.GetInt32(0),
                        ApartmentId = reader.GetInt32(1),
                        CheckIn = reader.GetDateTime(2),
                        CheckOut = reader.GetDateTime(3),
                        ApartmentAddress = reader.GetString(4),
                        PricePerDay = reader.GetDecimal(5)
                    });
                }

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при получении завершённых бронирований" });
            }
        }

        // ========== ЗАВЕРШИТЬ БРОНИРОВАНИЕ ==========
        [Authorize]
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteBooking(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Не авторизован" });

                var userId = int.Parse(userIdClaim.Value);

                // Проверяем, что бронирование принадлежит квартире этого арендодателя
                using var checkReader = await _dbService.DataAccess.GetBookingByIdAsync(id);
                if (!await checkReader.ReadAsync())
                    return NotFound(new { message = "Бронирование не найдено" });

                var apartmentId = checkReader.GetInt32(2);
                await checkReader.CloseAsync();

                // Проверяем, что квартира принадлежит этому арендодателю
                using var aptReader = await _dbService.DataAccess.GetApartmentByIdAsync(apartmentId);
                if (!await aptReader.ReadAsync())
                    return NotFound(new { message = "Квартира не найдена" });

                var landlordId = aptReader.GetInt32(1);
                await aptReader.CloseAsync();

                if (landlordId != userId)
                    return Forbid();

                // Завершаем бронирование
                var result = await _dbService.DataAccess.CompleteBookingAsync(id);
                if (result > 0)
                    return Ok(new { message = "Бронирование завершено" });
                else
                    return BadRequest(new { message = "Не удалось завершить бронирование" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при завершении бронирования" });
            }
        }
        [Authorize]
        [HttpPut("{id}/cancel-by-tenant")]
        public async Task<IActionResult> CancelBookingByTenant(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Не авторизован" });

                var userId = int.Parse(userIdClaim.Value);

                // Получаем бронирование
                using var bookingReader = await _dbService.DataAccess.GetBookingByIdAsync(id);
                if (!await bookingReader.ReadAsync())
                    return NotFound(new { message = "Бронирование не найдено" });

                var tenantId = bookingReader.GetInt32(bookingReader.GetOrdinal("arendator_polzovatel_id"));  // ID арендатора
                var apartmentId = bookingReader.GetInt32(bookingReader.GetOrdinal("kartochka_kvartiry_id")); // ID квартиры
                var statusId = bookingReader.GetInt32(bookingReader.GetOrdinal("status_id"));                // ID статуса

                await bookingReader.CloseAsync();

                // Проверяем, что бронирование принадлежит этому пользователю
                if (tenantId != userId)
                    return Forbid();

                // Статусы: 1 - Новая, 2 - Подтверждена, 3 - Отменена, 4 - Завершена и т.д.
                // Проверяем, можно ли отменить
                if (statusId == 4)  // Завершена
                    return BadRequest(new { message = "Нельзя отменить завершённое бронирование" });

                if (statusId == 3)  // Отменена
                    return BadRequest(new { message = "Бронирование уже отменено" });

                // Отменяем бронирование (устанавливаем status_id = 3)
                var result = await _dbService.DataAccess.CancelBookingAsync(id);
                if (result > 0)
                    return Ok(new { message = "Бронирование успешно отменено" });
                else
                    return BadRequest(new { message = "Не удалось отменить бронирование" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Ошибка при отмене: {ex.Message}" });
            }
        }
    }
}
