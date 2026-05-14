using Arenda.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Arenda.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    private readonly DatabaseService _dbService;

    public TestController(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "TestController работает!" });
    }

    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase()
    {
        var isConnected = await _dbService.TestConnectionAsync();
        if (isConnected)
            return Ok(new { message = "✅ Подключение к БД успешно!" });
        else
            return BadRequest(new { message = "❌ Ошибка подключения к БД" });
    }
    [HttpGet("users")]
    public async Task<IActionResult> TestGetUsers()
    {
        try
        {
            using var reader = await _dbService.DataAccess.GetUserByLoginAsync("admin", "admin");

            if (reader.HasRows)
            {
                await reader.ReadAsync();
                return Ok(new
                {
                    message = "Запрос выполнен",
                    hasData = true,
                    userId = reader.GetInt32(0),
                    login = reader.GetString(1)
                });
            }
            else
            {
                return Ok(new
                {
                    message = "Пользователь test не найден",
                    hasData = false
                });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
    [HttpGet("all-users")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            using var reader = await _dbService.DataAccess.GetAllUsersAsync();

            var users = new List<object>();
            while (await reader.ReadAsync())
            {
                users.Add(new
                {
                    id = reader.GetInt32(0),
                    login = reader.GetString(1),
                    email = reader.GetString(2),
                    name = $"{reader.GetString(3)} {reader.GetString(4)}"
                });
            }

            return Ok(new
            {
                count = users.Count,
                users = users
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
