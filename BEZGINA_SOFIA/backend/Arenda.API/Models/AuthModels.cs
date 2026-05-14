using System.ComponentModel.DataAnnotations;

namespace Arenda.API.Models
{
    // РЕГИСТРАЦИЯ
    public class RegisterRequest
    {
        [Required]
        [MinLength(3)]
        public string Login { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public bool IsLandlord { get; set; } // true - арендодатель, false - арендатор
    }

    // ВХОД
    public class LoginRequest
    {
        [Required]
        public string Login { get; set; }

        [Required]
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
        public string Email { get; set; }
        public string Login { get; set; }
    }
}
