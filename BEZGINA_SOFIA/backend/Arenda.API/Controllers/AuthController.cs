using Arenda.API.Models;
using Arenda.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Arenda.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly DatabaseService _dbService;

        public AuthController(UserService userService, DatabaseService dbService)
        {
            _userService = userService;
            _dbService = dbService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { message = "Не все поля заполнены", errors = ModelState });

                var result = await _userService.RegisterAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { message = "Введите логин и пароль" });

                var result = await _userService.LoginAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // POST: api/auth/forgot-password (шуточный)
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                Console.WriteLine("=== Шуточное восстановление пароля ===");
                Console.WriteLine($"Login: {request.Login}, Email: {request.Email}");

                // Используем _dbService.DataAccess напрямую
                using var reader = await _dbService.DataAccess.GetUserByLoginAsync(request.Login, request.Email);

                if (!await reader.ReadAsync())
                    return NotFound(new { message = "Пользователь не найден" });

                var login = reader.GetString(1); // логин

                await reader.CloseAsync();

                // Шуточный ответ (без реального пароля)
                return Ok(new
                {
                    oldPassword = "🤡 ШУТКА 🤡",
                    message = "Ваш пароль надёжно зашифрован и не может быть восстановлен. В реальном проекте вы получили бы ссылку для сброса пароля на email."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}    

