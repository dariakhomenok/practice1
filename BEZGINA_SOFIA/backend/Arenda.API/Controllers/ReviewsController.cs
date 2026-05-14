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
    public class ReviewsController : ControllerBase
    {
        private readonly DatabaseService _dbService;

        public ReviewsController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        // GET: api/reviews/apartment/5
        [HttpGet("apartment/{apartmentId}")]
        public async Task<IActionResult> GetApartmentReviews(int apartmentId)
        {
            try
            {
                var rating = await _dbService.DataAccess.GetApartmentRatingAsync(apartmentId);

                using var reader = await _dbService.DataAccess.GetApartmentReviewsAsync(apartmentId);
                var reviews = new List<object>();

                while (await reader.ReadAsync())
                {
                    reviews.Add(new
                    {
                        Id = reader.GetInt32(0),
                        TekstOtzyva = reader.GetString(1),
                        Otsenka = reader.GetInt32(2),
                        Date = reader.GetDateTime(3),
                        UserName = $"{reader.GetString(4)} {reader.GetString(5)}"
                    });
                }

                return Ok(new { rating = new { averageRating = rating, totalReviews = reviews.Count }, reviews = reviews });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return StatusCode(500, new { message = "Ошибка при получении отзывов о квартире" });
            }
        }

        // POST: api/reviews
        [Authorize] 
        [HttpPost]
        public async Task<IActionResult> AddReview([FromBody] AddReviewRequest request)
        {
            try
            {
                Console.WriteLine("=== ТЕСТ: создание отзыва без авторизации ===");
                Console.WriteLine($"Request: {System.Text.Json.JsonSerializer.Serialize(request)}");

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                Console.WriteLine($"UserId: {userId}");

                // Проверка: может ли пользователь оставить отзыв
                var canReview = await _dbService.DataAccess.CanUserReviewAsync(userId, request.BronirovaniyeId);
                Console.WriteLine($"CanReview: {canReview}");

                if (!canReview)
                    return BadRequest(new { message = "Вы не можете оставить отзыв. Отзыв можно оставить только после завершения бронирования, и только один раз" });

                var reviewId = await _dbService.DataAccess.AddReviewAsync(
                    request.BronirovaniyeId,
                    request.RatingId,
                    request.Text);

                Console.WriteLine($"ReviewId: {reviewId}");

                return Ok(new { id = reviewId, message = "Отзыв добавлен" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return StatusCode(500, new { message = "Ошибка при добавлении отзыва" });
            }
        }

        // GET: api/reviews/user/{userId}
        [Authorize]
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserReviews(int userId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized();

                var currentUserId = int.Parse(userIdClaim.Value);
                if (currentUserId != userId) return Forbid();

                using var reader = await _dbService.DataAccess.GetUserReviewsAsync(userId);
                var reviews = new List<object>();

                while (await reader.ReadAsync())
                {
                    reviews.Add(new
                    {
                        Id = reader.GetInt32(0),
                        ApartmentId = reader.GetInt32(1),
                        ApartmentAddress = reader.GetString(2),
                        Rating = reader.GetInt32(3),
                        Text = reader.GetString(4),
                        Date = reader.GetDateTime(5),
                        Odobreno = reader.GetBoolean(6),
                        BronirovaniyeId = reader.GetInt32(7)
                    });
                }

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при получении ваших отзывов" });
            }
        }

        // GET: api/reviews/landlord/{userId}
        [Authorize]
        [HttpGet("landlord/{userId}")]
        public async Task<IActionResult> GetLandlordReviews(int userId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized();

                var currentUserId = int.Parse(userIdClaim.Value);
                if (currentUserId != userId) return Forbid();

                using var reader = await _dbService.DataAccess.GetLandlordReviewsAsync(userId);
                var reviews = new List<object>();

                while (await reader.ReadAsync())
                {
                    reviews.Add(new
                    {
                        Id = reader.GetInt32(0),
                        ApartmentId = reader.GetInt32(1),
                        ApartmentAddress = reader.GetString(2),
                        Rating = reader.GetInt32(3),
                        Text = reader.GetString(4),
                        Date = reader.GetDateTime(5),
                        UserName = reader.GetString(6)
                    });
                }

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Ошибка при получении отзывов о ваших квартирах" });
            }
        }
    }

    public class AddReviewRequest
    {
        public int BronirovaniyeId { get; set; }
        public int RatingId { get; set; }
        public string Text { get; set; }
    }
}
