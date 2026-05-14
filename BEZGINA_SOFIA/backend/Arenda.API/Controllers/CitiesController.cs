using Microsoft.AspNetCore.Mvc;
using Arenda.API.Services;

namespace Arenda.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CitiesController : ControllerBase
    {
        private readonly DatabaseService _dbService;
        private readonly ILogger<CitiesController> _logger;

        public CitiesController(DatabaseService dbService, ILogger<CitiesController> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetCities()
        {
            try
            {
                _logger.LogInformation("Получение списка городов");

                var cities = new List<object>();

                using var reader = await _dbService.DataAccess.GetAllCitiesAsync();

                while (await reader.ReadAsync())
                {
                    cities.Add(new
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }

                return Ok(cities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка городов");
                return Ok(new List<object>()); // Возвращаем пустой список, чтобы не ломать Swagger
            }
        }
    }
}