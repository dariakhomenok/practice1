using Arenda.DBeaver;
using Npgsql;

namespace Arenda.API.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly DataDBeaver _dataAccess;  
        

        public DatabaseService(IConfiguration config)
        {
            _connectionString = Environment.GetEnvironmentVariable("ARENDA_DB_CONNECTION")
                ?? config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Не настроена строка подключения к базе данных");
            _dataAccess = new DataDBeaver(_connectionString);  
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public string ConnectionString => _connectionString;

        public DataDBeaver DataAccess => _dataAccess;  // ИСПРАВЛЕНО!
    }
}
