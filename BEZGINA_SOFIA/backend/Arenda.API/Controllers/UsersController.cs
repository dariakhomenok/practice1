using Arenda.API.Models;
using Arenda.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Arenda.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly DatabaseService _dbService;

        public UsersController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        // GET: api/users/{id}
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized();

                var currentUserId = int.Parse(userIdClaim.Value);
                if (currentUserId != id)
                    return Forbid();

                using var reader = await _dbService.DataAccess.GetUserByIdAsync(id);
                if (!await reader.ReadAsync())
                    return NotFound(new { message = "Пользователь не найден" });

                // Получаем отчество из таблицы arendator или arendodatel
                string patronymic = null;

                // Проверяем, является ли пользователь арендатором
                using var tenantReader = await _dbService.DataAccess.GetTenantByIdAsync(id);
                if (await tenantReader.ReadAsync())
                {
                    patronymic = tenantReader.IsDBNull(0) ? null : tenantReader.GetString(0);
                    await tenantReader.CloseAsync();
                }
                else
                {
                    await tenantReader.CloseAsync();
                    // Проверяем, является ли пользователь арендодателем
                    using var landlordReader = await _dbService.DataAccess.GetLandlordByIdAsync(id);
                    if (await landlordReader.ReadAsync())
                    {
                        patronymic = landlordReader.IsDBNull(0) ? null : landlordReader.GetString(0);
                        await landlordReader.CloseAsync();
                    }
                }

                var user = new
                {
                    Id = reader.GetInt32(0),
                    Login = reader.GetString(1),
                    Email = reader.GetString(3),
                    Phone = reader.GetString(4),
                    FirstName = reader.GetString(5),
                    LastName = reader.GetString(6),
                    Birthday = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7),
                    Patronymic = patronymic,
                    Photo = reader.GetString(8)
                };

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/users/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                Console.WriteLine("=== ОБНОВЛЕНИЕ ПОЛЬЗОВАТЕЛЯ ===");
                Console.WriteLine($"ID: {id}");
                Console.WriteLine($"FirstName: {request.FirstName}");
                Console.WriteLine($"LastName: {request.LastName}");
                Console.WriteLine($"Patronymic: {request.Patronymic}");
                Console.WriteLine($"Phone: {request.Phone}");
                Console.WriteLine($"Email: {request.Email}");
                Console.WriteLine($"Birthday: {request.Birthday}");

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized();

                var currentUserId = int.Parse(userIdClaim.Value);
                if (currentUserId != id)
                    return Forbid();

                await _dbService.DataAccess.UpdateUserAsync(
                    id,
                    request.Email,
                    request.Phone,
                    request.FirstName,
                    request.LastName,
                    request.Patronymic,
                    request.Birthday);

                return Ok(new { message = "Данные обновлены" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}