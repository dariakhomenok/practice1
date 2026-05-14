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
    public class ApartmentsController : ControllerBase
    {
        private readonly DatabaseService _dbService;

        public ApartmentsController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        // ========== 1. СПЕЦИФИЧЕСКИЕ МАРШРУТЫ (без параметров-чисел) - ПЕРВЫМИ! ==========

        [HttpGet("test-sql")]
        public async Task<IActionResult> TestSql()
        {
            try
            {
                using var conn = new NpgsqlConnection("Host=localhost;Port=5432;Database=postgres;Username=sofa;Password=AnImE0702");
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM kartochka_kvartiry", conn);
                var count = await cmd.ExecuteScalarAsync();

                return Ok(new { count = count, message = "Подключение к БД работает!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка подключения к базе данных", error = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchApartments([FromQuery] SearchApartmentsRequest request)
        {
            try
            {
                using var reader = await _dbService.DataAccess.SearchApartmentsWithFiltersAsync(
                    request.MinPrice, request.MaxPrice,
                    request.MinDeposit, request.MaxDeposit,
                    request.Rooms,
                    request.MinRentDays, request.MaxRentDays,
                    request.Furniture, request.Appliances, request.Internet, request.Parking,
                    request.Elevator, request.Balcony,
                    request.PetsAllowed, request.ChildrenAllowed, request.SmokingAllowed,
                    request.Renovation,
                    request.MinArea, request.MaxArea,
                    request.MinFloor, request.MaxFloor,
                    request.NotFirstFloor, request.NotLastFloor,
                    request.CityId,
                    request.AddressSearch);

                var apartments = new List<ApartmentListItemDto>();

                while (await reader.ReadAsync())
                {
                    apartments.Add(new ApartmentListItemDto
                    {
                        Id = reader.GetInt32(0),
                        Address = reader.GetString(1),
                        Price = reader.GetDecimal(2),
                        Area = reader.GetInt32(3),
                        Rooms = reader.GetInt32(4),
                        CityName = reader.GetString(5),
                        LandlordName = reader.GetString(6),
                        MainPhoto = reader.IsDBNull(7) ? null : reader.GetString(7)
                    });
                }

                return Ok(apartments);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при получении данных" });
            }
        }

        [HttpGet("landlord/{userId}")]
        public async Task<IActionResult> GetApartmentsByLandlord(int userId)
        {
            try
            {
                using var reader = await _dbService.DataAccess.GetApartmentsByLandlordAsync(userId);
                var apartments = new List<ApartmentListItemDto>();

                while (await reader.ReadAsync())
                {
                    apartments.Add(new ApartmentListItemDto
                    {
                        Id = reader.GetInt32(0),
                        Address = reader.GetString(1),
                        Price = reader.GetDecimal(2),
                        Area = reader.GetInt32(3),
                        Rooms = reader.GetInt32(4),
                        CityName = reader.GetString(5),
                        MainPhoto = reader.IsDBNull(6) ? null : reader.GetString(6)  // ← добавить
                    });
                }

                return Ok(apartments);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ========== 2. ОСНОВНОЙ GET (без параметров) ==========
        [HttpGet]
        public async Task<IActionResult> GetAllApartments()
        {
            try
            {
                using var reader = await _dbService.DataAccess.GetAllApartmentsAsync();
                var apartments = new List<ApartmentListItemDto>();

                while (await reader.ReadAsync())
                {
                    apartments.Add(new ApartmentListItemDto
                    {
                        Id = reader.GetInt32(0),
                        Address = reader.GetString(1),
                        Price = reader.GetDecimal(2),
                        Area = reader.GetInt32(3),
                        Rooms = reader.GetInt32(4),
                        CityName = reader.GetString(5),
                        LandlordName = reader.GetString(6),
                        MainPhoto = reader.IsDBNull(7) ? null : reader.GetString(7) 
                    });
                }

                return Ok(apartments);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при получении данных" });
            }
        }

        // ========== 3. GET С ПАРАМЕТРОМ {id} - В САМОМ КОНЦЕ! ==========
        [HttpGet("{id}")]
        public async Task<IActionResult> GetApartmentById(int id)
        {
            try
            {
                using var reader = await _dbService.DataAccess.GetApartmentDetailAsync(id);

                if (!await reader.ReadAsync())
                    return NotFound(new { message = "Квартира не найдена" });

                var apartment = new ApartmentDetailDto
                {
                    Id = reader.GetInt32(0),
                    LandlordId = reader.GetInt32(1),
                    CityId = reader.GetInt32(2),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Address = reader.GetString(4),
                    Area = reader.GetInt32(5),
                    Price = reader.GetDecimal(6),
                    Rooms = reader.GetInt32(7),
                    Floor = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    TotalFloors = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    Renovation = reader.IsDBNull(10) ? null : reader.GetString(10),
                    // ⚠️ smallint поля — читаем как int, потом преобразуем в bool
                    Furniture = reader.GetInt32(11) == 1,
                    Appliances = reader.GetInt32(12) == 1,
                    Internet = reader.GetInt32(13) == 1,
                    Parking = reader.GetInt32(14) == 1,
                    Elevator = reader.GetInt32(15) == 1,
                    Balcony = reader.GetInt32(16) == 1,
                    PetsAllowed = reader.GetInt32(17) == 1,
                    ChildrenAllowed = reader.GetInt32(18) == 1,
                    SmokingAllowed = reader.GetInt32(19) == 1,
                    MinRentDays = reader.GetInt32(20),
                    Deposit = reader.IsDBNull(21) ? null : reader.GetDecimal(21),
                    CityName = reader.GetString(22),
                    LandlordName = reader.GetString(23),
                    LandlordPhone = reader.GetString(24),
                    LandlordEmail = reader.GetString(25)
                };

                // Загружаем фото
                var photos = new List<PhotoDto>();
                using var photoReader = await _dbService.DataAccess.GetApartmentPhotosAsync(id);
                while (await photoReader.ReadAsync())
                {
                    photos.Add(new PhotoDto
                    {
                        Id = photoReader.GetInt32(0),
                        Url = photoReader.GetString(1),
                        Order = photoReader.GetInt32(2)
                    });
                }
                apartment.Photos = photos;

                return Ok(apartment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при получении данных" });
            }
        }

        // ========== 4. POST, PUT, DELETE - остаются внизу ==========
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateApartment([FromBody] CreateApartmentRequest request)
        {
            Console.WriteLine("=== ЗАПРОС ПОЛУЧЕН! ===");
            try
            {
                // ========== ОТЛАДКА ==========
                Console.WriteLine("=== CREATE APARTMENT ===");
                Console.WriteLine("Все claims в токене:");
                foreach (var claim in User.Claims)
                {
                    Console.WriteLine($"  {claim.Type} = {claim.Value}");
                }
                // =============================

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    Console.WriteLine("❌ Claim NameIdentifier не найден!");
                    return Unauthorized(new { message = "Не авторизован" });
                }

                var userId = int.Parse(userIdClaim.Value);
                Console.WriteLine($"✅ userId из токена: {userId}");

                var apartmentId = await _dbService.DataAccess.CreateApartmentAsync(
                    userId, request.CityId, request.Description, request.Address,
                    request.Area, request.Price, request.Rooms, request.Floor, request.TotalFloors,
                    request.Renovation,
                    request.Furniture, request.Appliances, request.Internet,
                    request.Parking, request.Elevator, request.Balcony, request.PetsAllowed,
                    request.ChildrenAllowed, request.SmokingAllowed, request.MinRentDays, request.Deposit
                );

                Console.WriteLine($"✅ Квартира создана, ID: {apartmentId}");
                return Ok(new { id = apartmentId, message = "Квартира успешно создана" });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return BadRequest(new { message = "Квартира с таким адресом уже существует" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
                return BadRequest(new { message = "Ошибка при создании квартиры. Проверьте правильность заполнения полей" });
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateApartment(int id, [FromBody] UpdateApartmentRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Не авторизован" });

                var userId = int.Parse(userIdClaim.Value);


                // Проверяем, что квартира принадлежит этому арендодателю
                using var checkReader = await _dbService.DataAccess.GetApartmentByIdAsync(id);
                if (!await checkReader.ReadAsync())
                    return NotFound(new { message = "Квартира не найдена" });

                var landlordId = checkReader.GetInt32(1);
                await checkReader.CloseAsync();

                if (landlordId != userId)
                    return Forbid();

                await _dbService.DataAccess.UpdateApartmentAsync(
                    id, request.CityId, request.Description, request.Address,
                    request.Area, request.Price, request.Rooms, request.Floor, request.TotalFloors,
                    request.Renovation,
                    request.Furniture, request.Appliances, request.Internet,
                    request.Parking, request.Elevator, request.Balcony, request.PetsAllowed,
                    request.ChildrenAllowed, request.SmokingAllowed, request.MinRentDays, request.Deposit
                );

                return Ok(new { message = "Квартира обновлена" });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return BadRequest(new { message = "Квартира с таким адресом уже существует" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при обновлении квартиры" });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApartment(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Не авторизован" });

                var userId = int.Parse(userIdClaim.Value);
                Console.WriteLine($"Удаление квартиры {id} пользователем {userId}");

                Console.WriteLine($"=== DELETE APARTMENT {id} ===");
                Console.WriteLine($"UserId из токена: {userId}");

                // Проверяем, что квартира принадлежит этому арендодателю
                using var checkReader = await _dbService.DataAccess.GetApartmentByIdAsync(id);
                if (!await checkReader.ReadAsync())
                    return NotFound(new { message = "Квартира не найдена" });

                var landlordId = checkReader.GetInt32(1);
                await checkReader.CloseAsync();

                if (landlordId != userId)
                    return Forbid();

                await _dbService.DataAccess.DeleteApartmentAsync(id);

                return Ok(new { message = "Квартира удалена" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при удалении квартиры" });
            }
        }

        // ========== ПОЛУЧИТЬ ЗАНЯТЫЕ ДАТЫ ДЛЯ КАЛЕНДАРЯ ==========
        [HttpGet("{id}/busy-dates")]
        public async Task<IActionResult> GetBusyDates(int id, [FromQuery] int year, [FromQuery] int month)
        {
            try
            {
                using var conn = new NpgsqlConnection(_dbService.ConnectionString);
                await conn.OpenAsync();

                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                using var cmd = new NpgsqlCommand(@"
                SELECT 
                    data_zayezda,
                    data_vyyezda
                FROM bronirovaniye
                WHERE kartochka_kvartiry_id = @apartmentId
                  AND status_id != (SELECT id FROM status_bronirovaniya WHERE nazvanie = 'Отменена')
                  AND data_zayezda <= @endDate
                  AND data_vyyezda >= @startDate", conn);

                cmd.Parameters.AddWithValue("@apartmentId", id);
                cmd.Parameters.AddWithValue("@startDate", startDate);
                cmd.Parameters.AddWithValue("@endDate", endDate);

                var busyDates = new List<DateTime>();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var checkIn = reader.GetDateTime(0);
                    var checkOut = reader.GetDateTime(1);

                    // Добавляем все дни от заезда до выезда (выезд не включаем)
                    for (var date = checkIn; date < checkOut; date = date.AddDays(1))
                    {
                        busyDates.Add(date.Date);
                    }
                }

                return Ok(busyDates.Select(d => d.ToString("yyyy-MM-dd")));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при получении занятых дат" });
            }
        }

        // ========== ЗАГРУЗКА ФОТОГРАФИЙ КВАРТИРЫ ==========
        [Authorize]
        [HttpPost("{id}/photos")]
        public async Task<IActionResult> UploadPhotos(int id, List<IFormFile> photos)
        {
            try
            {
                Console.WriteLine($"=== ЗАГРУЗКА ФОТО для квартиры {id} ===");
                Console.WriteLine($"Количество файлов: {photos?.Count ?? 0}");
                // 1. Проверяем авторизацию
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Не авторизован" });

                var userId = int.Parse(userIdClaim.Value);

                // 2. Проверяем, что квартира существует и принадлежит текущему пользователю
                using var checkReader = await _dbService.DataAccess.GetApartmentByIdAsync(id);
                if (!await checkReader.ReadAsync())
                    return NotFound(new { message = "Квартира не найдена" });

                var landlordId = checkReader.GetInt32(1);
                await checkReader.CloseAsync();

                if (landlordId != userId)
                    return Forbid();

                // 3. Проверяем, что есть фото для загрузки
                if (photos == null || photos.Count == 0)
                    return BadRequest(new { message = "Нет файлов для загрузки" });

                var uploadedPhotos = new List<PhotoDto>();
                int order = 0;

                // 4. Создаём папку для квартиры, если её нет
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "apartments", id.ToString());
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // 5. Сохраняем каждый файл
                foreach (var photo in photos)
                {
                    // Проверяем размер файла (максимум 5MB)
                    if (photo.Length > 5 * 1024 * 1024)
                        continue;

                    // Проверяем тип файла
                    var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg", "image/webp" };
                    if (!allowedTypes.Contains(photo.ContentType))
                        continue;

                    // Генерируем уникальное имя файла
                    var fileName = $"{Guid.NewGuid()}_{photo.FileName}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    // Сохраняем файл
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photo.CopyToAsync(stream);
                    }

                    // Формируем URL для доступа к фото
                    var photoUrl = $"/uploads/apartments/{id}/{fileName}";

                    // Сохраняем в БД
                    await _dbService.DataAccess.AddApartmentPhotoAsync(id, photoUrl, order);

                    uploadedPhotos.Add(new PhotoDto
                    {
                        Id = 0, // ID вернётся из БД
                        Url = photoUrl,
                        Order = order
                    });

                    order++;
                }

                return Ok(new
                {
                    message = $"Загружено {uploadedPhotos.Count} фотографий",
                    photos = uploadedPhotos
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при загрузке фотографий" });
            }
        }

        
    }
}