using System.ComponentModel.DataAnnotations;

namespace Arenda.API.Models
{
    // РЕГИСТРАЦИЯ
    public class RegisterRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        [MinLength(3)]
        public string Login { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(20)]
        [Phone]
        public string Phone { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        public bool IsLandlord { get; set; } // true - арендодатель, false - арендатор
    }

    // ВХОД
    public class LoginRequest
    {
        [Required]
        [StringLength(50)]
        public string Login { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }
    }

    // ОТВЕТ С ТОКЕНОМ
    public class AuthResponse
    {
        public int UserId { get; set; }
        public string Token { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool IsLandlord { get; set; }
    }

    // ДАННЫЕ ПОЛЬЗОВАТЕЛЯ (без пароля)
    public class UserDto
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(50)]
        public string Login { get; set; }
    }
}
