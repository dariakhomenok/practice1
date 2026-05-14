using Arenda.API.Models;
using Arenda.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Arenda.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AvatarController : ControllerBase
    {
        private readonly DatabaseService _dbService;

        public AvatarController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        // ЗАГРУЗКА АВАТАРА
        [Authorize]
        [HttpPost("{userId}")]
        public async Task<IActionResult> UploadAvatar(int userId, IFormFile avatar)
        {
            try
            {
                Console.WriteLine($"=== ЗАГРУЗКА АВАТАРА для пользователя {userId} ===");

                // Проверка авторизации
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Не авторизован" });

                var currentUserId = int.Parse(userIdClaim.Value);
                if (currentUserId != userId)
                    return Forbid();

                if (avatar == null)
                    return BadRequest(new { message = "Нет файла для загрузки" });

                // Проверка размера (максимум 5MB)
                if (avatar.Length > 5 * 1024 * 1024)
                    return BadRequest(new { message = "Файл слишком большой. Максимум 5MB" });

                // Проверка типа файла
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg", "image/webp" };
                if (!allowedTypes.Contains(avatar.ContentType))
                    return BadRequest(new { message = "Неподдерживаемый тип файла. Используйте JPEG, PNG или WEBP" });

                // Создаём папку для аватаров
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Генерируем уникальное имя файла
                var fileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Сохраняем файл
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                var avatarUrl = $"/uploads/avatars/{fileName}";

                // Сохраняем в БД
                await _dbService.DataAccess.UpdateUserAvatarAsync(userId, avatarUrl, fileName);

                Console.WriteLine($"Аватар сохранён: {avatarUrl}");
                return Ok(new { avatarUrl = avatarUrl, message = "Аватар загружен" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ПОЛУЧЕНИЕ АВАТАРА (возвращает URL)
        [AllowAnonymous]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetAvatar(int userId)
        {
            try
            {
                var avatarUrl = await _dbService.DataAccess.GetUserAvatarAsync(userId);

                if (string.IsNullOrEmpty(avatarUrl))
                    return NotFound(new { message = "Аватар не найден" });

                // Возвращаем полный URL
                var fullUrl = $"{Request.Scheme}://{Request.Host}{avatarUrl}";
                return Ok(new { avatarUrl = fullUrl, path = avatarUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения аватара: {ex.Message}");
                return NotFound(new { message = "Аватар не найден" });
            }
        }

        // УДАЛЕНИЕ АВАТАРА
        [Authorize]
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteAvatar(int userId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Не авторизован" });

                var currentUserId = int.Parse(userIdClaim.Value);
                if (currentUserId != userId)
                    return Forbid();

                // Получаем информацию об аватаре перед удалением
                var avatarUrl = await _dbService.DataAccess.GetUserAvatarAsync(userId);

                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    // Удаляем файл с диска
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", avatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }

                // Удаляем запись из БД
                await _dbService.DataAccess.DeleteUserAvatarAsync(userId);

                return Ok(new { message = "Аватар удалён" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления аватара: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}