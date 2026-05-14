using Arenda.API.Helpers;
using Arenda.API.Models;
using Npgsql;

namespace Arenda.API.Services
{
    public class UserService
    {
        private readonly DatabaseService _dbService;
        private readonly JwtHelper _jwtHelper;

        public UserService(DatabaseService dbService, JwtHelper jwtHelper)
        {
            _dbService = dbService;
            _jwtHelper = jwtHelper;
        }

        // РЕГИСТРАЦИЯ
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Проверяем, существует ли пользователь
            using var checkReader = await _dbService.DataAccess.GetUserByLoginAsync(request.Login, request.Email);
            if (await checkReader.ReadAsync())
            {
                throw new Exception("Пользователь с таким логином или email уже существует");
            }
            await checkReader.CloseAsync();

            // Хэшируем пароль
            var passwordHash = PasswordHelper.HashPassword(request.Password);

            // Создаём пользователя
            var userId = await _dbService.DataAccess.CreateUserAsync(
                request.Login,
                passwordHash,
                request.Email,
                request.Phone,
                request.FirstName,
                request.LastName
            );

            // ОПРЕДЕЛЯЕМ РОЛЬ И ДОБАВЛЯЕМ В СООТВЕТСТВУЮЩУЮ ТАБЛИЦУ
            int roleId;
            if (request.IsLandlord)
            {
                // Арендодатель
                await _dbService.DataAccess.CreateLandlordAsync(userId);
                roleId = 3; // tip_roli id = 3 для арендодателя (см. ваши данные)
            }
            else
            {
                // Арендатор
                await _dbService.DataAccess.CreateTenantAsync(userId);
                roleId = 4; // tip_roli id = 4 для арендатора
            }

            // Добавляем запись в rol_polzovatelya
            await _dbService.DataAccess.AddUserRoleAsync(userId, roleId);

            // Генерируем токен
            var token = _jwtHelper.GenerateToken(userId, request.Login, request.IsLandlord);

            return new AuthResponse
            {
                UserId = userId,
                Token = token,
                UserName = $"{request.FirstName} {request.LastName}",
                Email = request.Email,
                IsLandlord = request.IsLandlord
            };
        }

        // ВХОД
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // Ищем пользователя
            using var reader = await _dbService.DataAccess.GetUserByLoginAsync(request.Login, request.Login);

            if (!await reader.ReadAsync())
            {
                throw new Exception("Неверный логин или пароль");
            }

            var userId = reader.GetInt32(0);
            var login = reader.GetString(1);
            var passwordHash = reader.GetString(2);
            var email = reader.GetString(3);
            var phone = reader.GetString(4);
            var firstName = reader.GetString(5);
            var lastName = reader.GetString(6);

            await reader.CloseAsync();

            // Проверяем пароль
            if (!PasswordHelper.VerifyPassword(request.Password, passwordHash))
            {
                throw new Exception("Неверный логин или пароль");
            }

            // ОПРЕДЕЛЯЕМ РОЛЬ ПОЛЬЗОВАТЕЛЯ
            bool isLandlord = await CheckIfUserIsLandlord(userId);

            // Генерируем токен
            var token = _jwtHelper.GenerateToken(userId, login, isLandlord);

            return new AuthResponse
            {
                UserId = userId,
                Token = token,
                UserName = $"{firstName} {lastName}",
                Email = email,
                Phone = phone,
                IsLandlord = isLandlord
            };
        }
        // Вспомогательный метод для проверки роли
        private async Task<bool> CheckIfUserIsLandlord(int userId)
        {
            return await _dbService.DataAccess.IsUserLandlordAsync(userId);
        }

        // ПОЛУЧИТЬ ПОЛЬЗОВАТЕЛЯ ПО ID
        public async Task<UserDto> GetUserByIdAsync(int id)
        {
            using var reader = await _dbService.DataAccess.GetUserByIdAsync(id);

            if (!await reader.ReadAsync())
                return null;

            return new UserDto
            {
                Id = reader.GetInt32(0),
                Login = reader.GetString(1),
                Email = reader.GetString(3),
                Phone = reader.GetString(4),
                FirstName = reader.GetString(5),
                LastName = reader.GetString(6)
            };
        }
    }
}
