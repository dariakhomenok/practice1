using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arenda.DBeaver
{
    public class DataDBeaver
    {
        private readonly string _connectionString;

        public DataDBeaver(string connectionString)
        {
            _connectionString = connectionString;
        }
        public string ConnectionString => _connectionString;

        // ========== ПОЛЬЗОВАТЕЛИ ==========
        public async Task<NpgsqlDataReader> GetUserByLoginAsync(string login, string email)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(DatabaseQueries.GetUserByLogin, conn);
            cmd.Parameters.AddWithValue("@login", login);
            cmd.Parameters.AddWithValue("@email", email ?? login);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        public async Task<int> CreateUserAsync(string login, string password, string email, string telefon, string firstName, string lastName)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SyncSequenceAsync("polzovatel", "id");
            using var cmd = new NpgsqlCommand(DatabaseQueries.CreateUser, conn);
            cmd.Parameters.AddWithValue("@login", login);
            cmd.Parameters.AddWithValue("@parol", password);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@telefon", telefon);
            cmd.Parameters.AddWithValue("@imya", firstName);
            cmd.Parameters.AddWithValue("@familiya", lastName);
            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task<NpgsqlDataReader> GetUserByIdAsync(int id)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(DatabaseQueries.GetUserById, conn);
            cmd.Parameters.AddWithValue("@id", id);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        public async Task<int> UpdateUserAsync(int id, string email, string telefon, string firstName, string lastName, string patronymic, DateTime? birthday)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // 1. Обновляем основные данные в polzovatel
            using var cmd = new NpgsqlCommand(@"
            UPDATE polzovatel
            SET email = @email, 
                telefon = @telefon, 
                imya = @imya, 
                familiya = @familiya,
                data_rozhdeniya = @data_rozhdeniya
            WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@telefon", telefon);
            cmd.Parameters.AddWithValue("@imya", firstName);
            cmd.Parameters.AddWithValue("@familiya", lastName);
            cmd.Parameters.AddWithValue("@data_rozhdeniya", birthday ?? (object)DBNull.Value);

            int result = await cmd.ExecuteNonQueryAsync();

            // 2. Обновляем отчество в таблице arendator (если пользователь арендатор)
            using var updateTenantCmd = new NpgsqlCommand(@"
            UPDATE arendator 
            SET otchestvo = @otchestvo 
            WHERE polzovatel_id = @id", conn);
            updateTenantCmd.Parameters.AddWithValue("@otchestvo", patronymic ?? (object)DBNull.Value);
            updateTenantCmd.Parameters.AddWithValue("@id", id);
            await updateTenantCmd.ExecuteNonQueryAsync();

            // 3. Обновляем отчество в таблице arendodatel (если пользователь арендодатель)
            using var updateLandlordCmd = new NpgsqlCommand(@"
            UPDATE arendodatel 
            SET otchestvo = @otchestvo 
            WHERE polzovatel_id = @id", conn);
            updateLandlordCmd.Parameters.AddWithValue("@otchestvo", patronymic ?? (object)DBNull.Value);
            updateLandlordCmd.Parameters.AddWithValue("@id", id);
            await updateLandlordCmd.ExecuteNonQueryAsync();

            return result;
        }

        // проверка на арендатора
        public async Task<bool> IsTenantAsync(int userId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM arendator WHERE polzovatel_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", userId);
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task<NpgsqlDataReader> GetAllUsersAsync()
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand("SELECT id, login, email, imya, familiya FROM polzovatel", conn);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // Добавить арендодателя
        public async Task<int> CreateLandlordAsync(int userId, string otchestvo = null)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
            INSERT INTO arendodatel (polzovatel_id, otchestvo, kolichestvo_obyavleniy)
            VALUES (@userId, @otchestvo, 0)
            RETURNING polzovatel_id;", conn);

            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@otchestvo", otchestvo ?? (object)DBNull.Value);

            return (int)await cmd.ExecuteScalarAsync();
        }

        // Добавить арендатора
        public async Task<int> CreateTenantAsync(int userId, string otchestvo = null)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
            INSERT INTO arendator (polzovatel_id, otchestvo)
            VALUES (@userId, @otchestvo)
            RETURNING polzovatel_id;", conn);

            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@otchestvo", otchestvo ?? (object)DBNull.Value);

            return (int)await cmd.ExecuteScalarAsync();
        }

        // Добавить связь с ролью в rol_polzovatelya
        public async Task<int> AddUserRoleAsync(int userId, int roleId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Получаем максимальный id
            var maxIdCmd = new NpgsqlCommand("SELECT COALESCE(MAX(id), 0) + 1 FROM rol_polzovatelya", conn);
            int newId = Convert.ToInt32(await maxIdCmd.ExecuteScalarAsync());

            using var cmd = new NpgsqlCommand(@"
            INSERT INTO rol_polzovatelya (id, polzovatel_id, tip_roli_id, data_naznacheniya)
            VALUES (@id, @userId, @roleId, @date)
            RETURNING id;", conn);

            cmd.Parameters.AddWithValue("@id", newId);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@roleId", roleId);
            cmd.Parameters.AddWithValue("@date", DateTime.Now);

            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task<bool> IsUserLandlordAsync(int userId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM arendodatel WHERE polzovatel_id = @userId", conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        // Получить отчество арендатора
        public async Task<NpgsqlDataReader> GetTenantByIdAsync(int userId)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand("SELECT otchestvo FROM arendator WHERE polzovatel_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", userId);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // Получить отчество арендодателя
        public async Task<NpgsqlDataReader> GetLandlordByIdAsync(int userId)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand("SELECT otchestvo FROM arendodatel WHERE polzovatel_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", userId);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // ========== АВАТАРЫ ==========

        // Получить аватар (возвращает только URL)
        public async Task<string> GetUserAvatarAsync(int userId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(DatabaseQueries.GetUserAvatar, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            var result = await cmd.ExecuteScalarAsync();
            return result as string;
        }

        // Обновить аватар
        public async Task<int> UpdateUserAvatarAsync(int userId, string photoUrl, string fileName)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(DatabaseQueries.UpdateUserAvatar, conn);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@photoUrl", photoUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@fileName", fileName ?? (object)DBNull.Value);

            return await cmd.ExecuteNonQueryAsync();
        }

        // Удалить аватар
        public async Task<int> DeleteUserAvatarAsync(int userId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(DatabaseQueries.DeleteUserAvatar, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            return await cmd.ExecuteNonQueryAsync();
        }



        // ========== КВАРТИРЫ ==========

        // 1. Получить все квартиры (базовый метод)
        public async Task<NpgsqlDataReader> GetAllApartmentsAsync()
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(@"
            SELECT 
                kk.id,
                kk.adres_kvartiry,
                kk.tsena,
                kk.ploshad,
                kk.komnat,
                g.nazvanie_goroda,
                p.imya || ' ' || p.familiya AS landlord_name,
                (SELECT url_photo FROM photo_kvartiry 
                 WHERE kartochka_kvartiry_id = kk.id 
                 ORDER BY poryadok LIMIT 1) AS main_photo
            FROM kartochka_kvartiry kk
            JOIN gorod g ON kk.gorod_id = g.id
            JOIN arendodatel a ON kk.arendodatel_id = a.polzovatel_id
            JOIN polzovatel p ON a.polzovatel_id = p.id
            ORDER BY kk.id DESC", conn);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // 2. Получить квартиру по ID
        public async Task<NpgsqlDataReader> GetApartmentByIdAsync(int id)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(DatabaseQueries.GetApartmentById, conn);
            cmd.Parameters.AddWithValue("@id", id);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // 3. Получить квартиры арендодателя
        public async Task<NpgsqlDataReader> GetApartmentsByLandlordAsync(int userId)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(@"
            SELECT 
                kk.id,
                kk.adres_kvartiry,
                kk.tsena,
                kk.ploshad,
                kk.komnat,
                g.nazvanie_goroda,
                (SELECT url_photo FROM photo_kvartiry 
                 WHERE kartochka_kvartiry_id = kk.id 
                 ORDER BY poryadok LIMIT 1) AS main_photo
            FROM kartochka_kvartiry kk
            JOIN gorod g ON kk.gorod_id = g.id
            WHERE kk.arendodatel_id = @userId
            ORDER BY kk.id DESC", conn);

            cmd.Parameters.AddWithValue("@userId", userId);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // 4. Создать квартиру
        public async Task<int> CreateApartmentAsync(
            int landlordId, int cityId, string description, string address,
            int area, decimal price, int rooms, int? floor, int? totalFloors,
            string renovation,
            bool furniture, bool appliances, bool internet, bool parking,
            bool elevator, bool balcony, bool petsAllowed, bool childrenAllowed,
            bool smokingAllowed, int minRentDays, decimal? deposit)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await SyncSequenceAsync("kartochka_kvartiry", "id");
            using var cmd = new NpgsqlCommand(DatabaseQueries.CreateApartment, conn);
            cmd.Parameters.AddWithValue("@arendodatel_id", landlordId);
            cmd.Parameters.AddWithValue("@gorod_id", cityId);
            cmd.Parameters.AddWithValue("@opisanie", description);
            cmd.Parameters.AddWithValue("@adres", address);
            cmd.Parameters.AddWithValue("@ploschad", area);
            cmd.Parameters.AddWithValue("@tsena", price);
            cmd.Parameters.AddWithValue("@komnat", rooms);
            cmd.Parameters.AddWithValue("@etazh", floor ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@etazhnost", totalFloors ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@remont", renovation ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@mebel", furniture ? 1 : 0);
            cmd.Parameters.AddWithValue("@tehnika", appliances ? 1 : 0);
            cmd.Parameters.AddWithValue("@internet", internet ? 1 : 0);
            cmd.Parameters.AddWithValue("@parkovka", parking ? 1 : 0);
            cmd.Parameters.AddWithValue("@lift", elevator ? 1 : 0);
            cmd.Parameters.AddWithValue("@balkon", balcony ? 1 : 0);
            cmd.Parameters.AddWithValue("@zhivotnye_dopustimy", petsAllowed ? 1 : 0);
            cmd.Parameters.AddWithValue("@deti_dopustimy", childrenAllowed ? 1 : 0);
            cmd.Parameters.AddWithValue("@kuriye_dopustimo", smokingAllowed ? 1 : 0);
            cmd.Parameters.AddWithValue("@min_srok", minRentDays);
            cmd.Parameters.AddWithValue("@zalog", deposit ?? (object)DBNull.Value);
            return (int)await cmd.ExecuteScalarAsync();
        }

        // 5. Обновить квартиру
        public async Task<int> UpdateApartmentAsync(
            int id, int cityId, string description, string address,
            int area, decimal price, int rooms, int? floor, int? totalFloors,
            string renovation,
            bool furniture, bool appliances, bool internet, bool parking,
            bool elevator, bool balcony, bool petsAllowed, bool childrenAllowed,
            bool smokingAllowed, int minRentDays, decimal? deposit)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(DatabaseQueries.UpdateApartment, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@gorod_id", cityId);
            cmd.Parameters.AddWithValue("@opisanie", description);
            cmd.Parameters.AddWithValue("@adres", address);
            cmd.Parameters.AddWithValue("@ploschad", area);
            cmd.Parameters.AddWithValue("@tsena", price);
            cmd.Parameters.AddWithValue("@komnat", rooms);
            cmd.Parameters.AddWithValue("@etazh", floor ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@etazhnost", totalFloors ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@remont", renovation ?? (object)DBNull.Value);

            // ⚠️ Преобразуем bool в int (1 или 0)
            cmd.Parameters.AddWithValue("@mebel", furniture ? 1 : 0);
            cmd.Parameters.AddWithValue("@tehnika", appliances ? 1 : 0);
            cmd.Parameters.AddWithValue("@internet", internet ? 1 : 0);
            cmd.Parameters.AddWithValue("@parkovka", parking ? 1 : 0);
            cmd.Parameters.AddWithValue("@lift", elevator ? 1 : 0);
            cmd.Parameters.AddWithValue("@balkon", balcony ? 1 : 0);
            cmd.Parameters.AddWithValue("@zhivotnye_dopustimy", petsAllowed ? 1 : 0);
            cmd.Parameters.AddWithValue("@deti_dopustimy", childrenAllowed ? 1 : 0);
            cmd.Parameters.AddWithValue("@kuriye_dopustimo", smokingAllowed ? 1 : 0);

            cmd.Parameters.AddWithValue("@min_srok", minRentDays);
            cmd.Parameters.AddWithValue("@zalog", deposit ?? (object)DBNull.Value);

            return (int)await cmd.ExecuteScalarAsync();
        }

        // 6. Удалить квартиру
        public async Task<int> DeleteApartmentAsync(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(DatabaseQueries.DeleteApartment, conn);
            cmd.Parameters.AddWithValue("@id", id);
            return await cmd.ExecuteNonQueryAsync();
        }

        // ========== ФОТОГРАФИИ КВАРТИР ==========
        public async Task<NpgsqlDataReader> GetApartmentPhotosAsync(int apartmentId)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(@"
            SELECT id, url_photo, poryadok
            FROM photo_kvartiry
            WHERE kartochka_kvartiry_id = @apartmentId
            ORDER BY poryadok", conn);

            cmd.Parameters.AddWithValue("@apartmentId", apartmentId);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // ========== ПОЛУЧИТЬ ВСЕ КВАРТИРЫ С ИМЕНЕМ АРЕНДОДАТЕЛЯ ==========
        public async Task<NpgsqlDataReader> GetAllApartmentsWithLandlordAsync()
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(@"
            SELECT 
                kk.id,
                kk.adres_kvartiry,
                kk.tsena,
                kk.ploshad,
                kk.komnat,
                g.nazvanie_goroda,
                p.imya || ' ' || p.familiya
            FROM kartochka_kvartiry kk
            JOIN gorod g ON kk.gorod_id = g.id
            JOIN arendodatel a ON kk.arendodatel_id = a.polzovatel_id
            JOIN polzovatel p ON a.polzovatel_id = p.id
            ORDER BY kk.id DESC", conn);

            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // ========== ПОЛУЧИТЬ КВАРТИРУ С ПОЛНЫМИ ДАННЫМИ ==========
        public async Task<NpgsqlDataReader> GetApartmentDetailAsync(int id)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(@"
            SELECT 
                kk.id,
                kk.arendodatel_id,
                kk.gorod_id,
                kk.opisaniye_kvartiry,
                kk.adres_kvartiry,
                kk.ploshad,
                kk.tsena,
                kk.komnat,
                kk.etazh,
                kk.etazhnost,
                kk.remont,
                kk.mebel,
                kk.tehnika,
                kk.internet,
                kk.parkovka,
                kk.lift,
                kk.balkon,
                kk.zhivotnye_dopustimo,
                kk.deti_dopustimy,
                kk.kuriye_dopustimo,
                kk.min_srok_v_sutkah,
                kk.zalog,
                g.nazvanie_goroda,
                p.imya || ' ' || p.familiya,
                p.telefon,
                p.email
            FROM kartochka_kvartiry kk
            JOIN gorod g ON kk.gorod_id = g.id
            JOIN arendodatel a ON kk.arendodatel_id = a.polzovatel_id
            JOIN polzovatel p ON a.polzovatel_id = p.id
            WHERE kk.id = @id", conn);

            cmd.Parameters.AddWithValue("@id", id);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        public async Task<NpgsqlDataReader> SearchApartmentsWithFiltersAsync(
            decimal? minPrice, decimal? maxPrice,
            decimal? minDeposit, decimal? maxDeposit,
            int? rooms,
            int? minRentDays, int? maxRentDays,
            bool? furniture, bool? appliances, bool? internet, bool? parking,
            bool? elevator, bool? balcony,
            bool? petsAllowed, bool? childrenAllowed, bool? smokingAllowed,
            string? renovation,
            int? minArea, int? maxArea,
            int? minFloor, int? maxFloor,
            bool? notFirstFloor, bool? notLastFloor,
            int? cityId,
            string? addressSearch)
                {
                    var conn = new NpgsqlConnection(_connectionString);
                    await conn.OpenAsync();

                    var query = @"
                SELECT 
                    kk.id,
                    kk.adres_kvartiry,
                    kk.tsena,
                    kk.ploshad,
                    kk.komnat,
                    g.nazvanie_goroda,
                    p.imya || ' ' || p.familiya,
                    (SELECT url_photo FROM photo_kvartiry 
                     WHERE kartochka_kvartiry_id = kk.id 
                     ORDER BY poryadok LIMIT 1) AS main_photo
                FROM kartochka_kvartiry kk
                JOIN gorod g ON kk.gorod_id = g.id
                JOIN arendodatel a ON kk.arendodatel_id = a.polzovatel_id
                JOIN polzovatel p ON a.polzovatel_id = p.id
                WHERE 1=1";

            // Цена
            if (minPrice.HasValue)
                query += " AND kk.tsena >= @minPrice";
            if (maxPrice.HasValue)
                query += " AND kk.tsena <= @maxPrice";

            // Залог
            if (minDeposit.HasValue)
                query += " AND kk.zalog >= @minDeposit";
            if (maxDeposit.HasValue)
                query += " AND kk.zalog <= @maxDeposit";

            // Комнаты
            if (rooms.HasValue)
            {
                if (rooms.Value == 0) // Студия
                    query += " AND kk.komnat = 0";
                else if (rooms.Value == 4) // 4+ комнаты
                    query += " AND kk.komnat >= 4";
                else
                    query += " AND kk.komnat = @rooms";
            }

            // Срок аренды
            if (minRentDays.HasValue)
                query += " AND kk.min_srok_v_sutkah >= @minRentDays";
            if (maxRentDays.HasValue)
                query += " AND kk.min_srok_v_sutkah <= @maxRentDays";

            // Удобства
            if (furniture.HasValue)
                query += furniture.Value ? " AND kk.mebel = 1" : " AND kk.mebel = 0";
            if (appliances.HasValue)
                query += appliances.Value ? " AND kk.tehnika = 1" : " AND kk.tehnika = 0";
            if (internet.HasValue)
                query += internet.Value ? " AND kk.internet = 1" : " AND kk.internet = 0";
            if (parking.HasValue)
                query += parking.Value ? " AND kk.parkovka = 1" : " AND kk.parkovka = 0";
            if (elevator.HasValue)
                query += elevator.Value ? " AND kk.lift = 1" : " AND kk.lift = 0";
            if (balcony.HasValue)
                query += balcony.Value ? " AND kk.balkon = 1" : " AND kk.balkon = 0";

            // Правила проживания
            if (petsAllowed.HasValue)
                query += petsAllowed.Value ? " AND kk.zhivotnye_dopustimo = 1" : " AND kk.zhivotnye_dopustimo = 0";
            if (childrenAllowed.HasValue)
                query += childrenAllowed.Value ? " AND kk.deti_dopustimy = 1" : " AND kk.deti_dopustimy = 0";
            if (smokingAllowed.HasValue)
                query += smokingAllowed.Value ? " AND kk.kuriye_dopustimo = 1" : " AND kk.kuriye_dopustimo = 0";

            // Ремонт
            if (!string.IsNullOrEmpty(renovation))
                query += " AND kk.remont = @renovation";

            // Площадь
            if (minArea.HasValue)
                query += " AND kk.ploshad >= @minArea";
            if (maxArea.HasValue)
                query += " AND kk.ploshad <= @maxArea";

            // Этаж
            if (minFloor.HasValue)
                query += " AND kk.etazh >= @minFloor";
            if (maxFloor.HasValue)
                query += " AND kk.etazh <= @maxFloor";
            if (notFirstFloor.HasValue && notFirstFloor.Value)
                query += " AND kk.etazh > 1";
            if (notLastFloor.HasValue && notLastFloor.Value)
                query += " AND kk.etazh < kk.etazhnost";

            // Город
            if (cityId.HasValue)
                query += " AND kk.gorod_id = @cityId";

            // Поиск по адресу
            if (!string.IsNullOrEmpty(addressSearch))
                query += " AND kk.adres_kvartiry ILIKE @addressSearch";

            query += " ORDER BY kk.id DESC";

            var cmd = new NpgsqlCommand(query, conn);

            // Параметры
            if (minPrice.HasValue) cmd.Parameters.AddWithValue("@minPrice", minPrice.Value);
            if (maxPrice.HasValue) cmd.Parameters.AddWithValue("@maxPrice", maxPrice.Value);
            if (minDeposit.HasValue) cmd.Parameters.AddWithValue("@minDeposit", minDeposit.Value);
            if (maxDeposit.HasValue) cmd.Parameters.AddWithValue("@maxDeposit", maxDeposit.Value);
            if (rooms.HasValue && rooms.Value > 0 && rooms.Value < 4)
                cmd.Parameters.AddWithValue("@rooms", rooms.Value);
            if (minRentDays.HasValue) cmd.Parameters.AddWithValue("@minRentDays", minRentDays.Value);
            if (maxRentDays.HasValue) cmd.Parameters.AddWithValue("@maxRentDays", maxRentDays.Value);
            if (!string.IsNullOrEmpty(renovation)) cmd.Parameters.AddWithValue("@renovation", renovation);
            if (minArea.HasValue) cmd.Parameters.AddWithValue("@minArea", minArea.Value);
            if (maxArea.HasValue) cmd.Parameters.AddWithValue("@maxArea", maxArea.Value);
            if (minFloor.HasValue) cmd.Parameters.AddWithValue("@minFloor", minFloor.Value);
            if (maxFloor.HasValue) cmd.Parameters.AddWithValue("@maxFloor", maxFloor.Value);
            if (cityId.HasValue) cmd.Parameters.AddWithValue("@cityId", cityId.Value);
            if (!string.IsNullOrEmpty(addressSearch))
                cmd.Parameters.AddWithValue("@addressSearch", $"%{addressSearch}%");

            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // ========== ДОБАВИТЬ ФОТОГРАФИЮ КВАРТИРЫ ==========
        public async Task<int> AddApartmentPhotoAsync(int apartmentId, string url, int order)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await SyncPhotoSequenceAsync();

            using var cmd = new NpgsqlCommand(@"
            INSERT INTO photo_kvartiry (kartochka_kvartiry_id, url_photo, poryadok)
            VALUES (@apartmentId, @url, @order)
            RETURNING id;", conn);

            cmd.Parameters.AddWithValue("@apartmentId", apartmentId);
            cmd.Parameters.AddWithValue("@url", url);
            cmd.Parameters.AddWithValue("@order", order);

            try
            {
                var result = await cmd.ExecuteScalarAsync();
                Console.WriteLine($"Фото добавлено: ID={result}, URL={url}");
                return (int)result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении фото: {ex.Message}");
                throw;
            }
        }

        private async Task SyncPhotoSequenceAsync()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
        SELECT setval('photo_kvartiry_id_seq', 
                     (SELECT COALESCE(MAX(id), 0) + 1 FROM photo_kvartiry), 
                     false)", conn);

            await cmd.ExecuteNonQueryAsync();
        }


        // ========== БРОНИРОВАНИЯ ==========
        public async Task<int> CheckAvailabilityAsync(int apartmentId, DateTime checkIn, DateTime checkOut)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(DatabaseQueries.CheckAvailability, conn);
            cmd.Parameters.AddWithValue("@apartment_id", apartmentId);
            cmd.Parameters.AddWithValue("@check_in", checkIn);
            cmd.Parameters.AddWithValue("@check_out", checkOut);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // синхронизация айдишников бронирования
        private async Task SyncBronirovaniyeSequenceAsync()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
        SELECT setval('bronirovaniye_id_seq', 
                     (SELECT COALESCE(MAX(id), 0) + 1 FROM bronirovaniye), 
                     false)", conn);

            await cmd.ExecuteNonQueryAsync();
        }
        // Создать бронирование
        public async Task<int> CreateBookingAsync(int userId, int apartmentId, DateTime checkIn, DateTime checkOut)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await SyncBronirovaniyeSequenceAsync();

            using var cmd = new NpgsqlCommand(@"
            INSERT INTO bronirovaniye (arendator_polzovatel_id, kartochka_kvartiry_id, 
                                        data_zayezda, data_vyyezda, 
                                        status_id, created_at)
            VALUES (@userId, @apartmentId, @checkIn, @checkOut,
                    (SELECT id FROM status_bronirovaniya WHERE nazvanie = 'Новая'), NOW())
            RETURNING id", conn);

            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@apartmentId", apartmentId);
            cmd.Parameters.AddWithValue("@checkIn", checkIn);
            cmd.Parameters.AddWithValue("@checkOut", checkOut);

            return (int)await cmd.ExecuteScalarAsync();
        }

        // Получить бронирования арендодателя
        public async Task<NpgsqlDataReader> GetLandlordBookingsAsync(int landlordId)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(@"
            SELECT 
                b.id,
                b.kartochka_kvartiry_id,
                kk.adres_kvartiry,
                b.arendator_polzovatel_id,
                p.imya || ' ' || p.familiya AS tenant_name,
                p.telefon AS tenant_phone,
                b.data_zayezda,
                b.data_vyyezda,
                s.nazvanie AS status,
                kk.tsena * (b.data_vyyezda - b.data_zayezda) AS total_price,
                b.created_at
            FROM bronirovaniye b
            JOIN kartochka_kvartiry kk ON b.kartochka_kvartiry_id = kk.id
            JOIN polzovatel p ON b.arendator_polzovatel_id = p.id
            JOIN status_bronirovaniya s ON b.status_id = s.id
            WHERE kk.arendodatel_id = @landlordId
            ORDER BY b.data_zayezda ASC", conn);
            cmd.Parameters.AddWithValue("@landlordId", landlordId);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        public async Task<int> CancelBookingAsync(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
            UPDATE bronirovaniye 
            SET status_id = (SELECT id FROM status_bronirovaniya WHERE nazvanie = 'Отменена')
            WHERE id = @id
            RETURNING id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? (int)result : 0;
        }

        // получить бронирования по ID
        public async Task<NpgsqlDataReader> GetBookingByIdAsync(int id)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(@"
            SELECT b.id, b.arendator_polzovatel_id, b.kartochka_kvartiry_id, 
                   b.data_zayezda, b.data_vyyezda, b.status_id, b.created_at
            FROM bronirovaniye b
            WHERE b.id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // Подтвердить бронирование
        public async Task<int> ConfirmBookingAsync(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
            UPDATE bronirovaniye 
            SET status_id = (SELECT id FROM status_bronirovaniya WHERE nazvanie = 'Подтверждена')
            WHERE id = @id AND status_id = (SELECT id FROM status_bronirovaniya WHERE nazvanie = 'Новая')
            RETURNING id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? (int)result : 0;
        }

        // Получить бронирования арендатора
        public async Task<NpgsqlDataReader> GetUserBookingsAsync(int userId)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(@"
            SELECT 
                b.id,
                b.kartochka_kvartiry_id,
                kk.adres_kvartiry,
                kk.tsena,
                b.data_zayezda,
                b.data_vyyezda,
                s.nazvanie AS status,
                kk.tsena * (b.data_vyyezda - b.data_zayezda) AS total_price,
                b.created_at
            FROM bronirovaniye b
            JOIN kartochka_kvartiry kk ON b.kartochka_kvartiry_id = kk.id
            JOIN status_bronirovaniya s ON b.status_id = s.id
            WHERE b.arendator_polzovatel_id = @userId
            ORDER BY b.data_zayezda ASC", conn);
            cmd.Parameters.AddWithValue("@userId", userId);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // Проверка доступности квартиры
        public async Task<bool> IsApartmentAvailableAsync(int apartmentId, DateTime checkIn, DateTime checkOut)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM bronirovaniye
            WHERE kartochka_kvartiry_id = @apartmentId
              AND status_id != (SELECT id FROM status_bronirovaniya WHERE nazvanie = 'Отменена')
              AND data_zayezda < @checkOut
              AND data_vyyezda > @checkIn", conn);

            cmd.Parameters.AddWithValue("@apartmentId", apartmentId);
            cmd.Parameters.AddWithValue("@checkIn", checkIn);
            cmd.Parameters.AddWithValue("@checkOut", checkOut);

            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count == 0;
        }

        // Завершить бронирование
        public async Task<int> CompleteBookingAsync(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
        UPDATE bronirovaniye 
        SET status_id = (SELECT id FROM status_bronirovaniya WHERE nazvanie = 'Завершена')
        WHERE id = @id 
          AND status_id = (SELECT id FROM status_bronirovaniya WHERE nazvanie = 'Подтверждена')
        RETURNING id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? (int)result : 0;
        }

        // ========== СТАТИСТИКА ==========
        public async Task<int> GetLandlordApartmentsCountAsync(int userId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(DatabaseQueries.GetLandlordApartmentsCount, conn);
            cmd.Parameters.AddWithValue("@user_id", userId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<int> GetLandlordActiveBookingsCountAsync(int userId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(DatabaseQueries.GetLandlordActiveBookingsCount, conn);
            cmd.Parameters.AddWithValue("@user_id", userId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<decimal> GetLandlordMonthlyIncomeAsync(int userId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(DatabaseQueries.GetLandlordMonthlyIncome, conn);
            cmd.Parameters.AddWithValue("@user_id", userId);
            return Convert.ToDecimal(await cmd.ExecuteScalarAsync());
        }

        // ========== ПОИСК ==========
        public async Task<NpgsqlDataReader> SearchApartmentsAsync(int? cityId, decimal minPrice, decimal maxPrice, int rooms, DateTime checkIn, DateTime checkOut)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(DatabaseQueries.SearchApartments, conn);
            cmd.Parameters.AddWithValue("@city_id", cityId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@min_price", minPrice);
            cmd.Parameters.AddWithValue("@max_price", maxPrice);
            cmd.Parameters.AddWithValue("@rooms", rooms);
            cmd.Parameters.AddWithValue("@check_in", checkIn);
            cmd.Parameters.AddWithValue("@check_out", checkOut);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // ========== СИНХРОНИЗАЦИЯ ==========

        private async Task SyncSequenceAsync(string tableName, string columnName)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand($@"
        SELECT setval(pg_get_serial_sequence('{tableName}', '{columnName}'), 
                     COALESCE(MAX({columnName}), 0) + 1, 
                     false) 
        FROM {tableName}", conn);

            await cmd.ExecuteNonQueryAsync();
        }


        // ========== ОТЗЫВЫ И РЕЙТИНГ ==========

        // Получить рейтинг квартиры
        public async Task<double> GetApartmentRatingAsync(int apartmentId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
            SELECT COALESCE(AVG(rk.otsenka), 0)
            FROM otzivy o
            JOIN reyting_klassifikator rk ON rk.id = o.reyting_klassifikator_id
            JOIN bronirovaniye b ON b.id = o.bronirovaniye_id
            WHERE b.kartochka_kvartiry_id = @id AND o.odobreno = B'1'", conn);  // ← ВЕРНУЛ с B'1'
            cmd.Parameters.AddWithValue("@id", apartmentId);
            return Convert.ToDouble(await cmd.ExecuteScalarAsync());
        }
        // Получить отзывы квартиры (только одобренные)
        public async Task<NpgsqlDataReader> GetApartmentReviewsAsync(int apartmentId)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
            SELECT o.id, o.tekst_otzyva, rk.otsenka, o.date,
                   p.imya, p.familiya
            FROM otzivy o
            JOIN reyting_klassifikator rk ON rk.id = o.reyting_klassifikator_id
            JOIN bronirovaniye b ON b.id = o.bronirovaniye_id
            JOIN polzovatel p ON p.id = b.arendator_polzovatel_id
            WHERE b.kartochka_kvartiry_id = @id AND o.odobreno = B'1'
            ORDER BY o.date DESC";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", apartmentId);

            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // Добавить отзыв
        public async Task<int> AddReviewAsync(int bronirovaniyeId, int ratingId, string text)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Синхронизация sequence
            using var syncCmd = new NpgsqlCommand(@"
                SELECT setval('otzivy_id_seq', COALESCE((SELECT MAX(id) FROM otzivy), 0) + 1, false)", conn);
            await syncCmd.ExecuteNonQueryAsync();

            using var cmd = new NpgsqlCommand(@"
                INSERT INTO otzivy (id, bronirovaniye_id, reyting_klassifikator_id, tekst_otzyva, odobreno)
                VALUES (nextval('otzivy_id_seq'), @b, @r, @t, B'1')
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("@b", bronirovaniyeId);
            cmd.Parameters.AddWithValue("@r", ratingId);
            cmd.Parameters.AddWithValue("@t", text);

            return (int)await cmd.ExecuteScalarAsync();
        }

        // Проверка
        public async Task<bool> CanUserReviewAsync(int userId, int bronirovaniyeId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
        SELECT EXISTS(
            SELECT 1 FROM bronirovaniye b
            WHERE b.id = @bid 
              AND b.arendator_polzovatel_id = @uid
              AND b.data_vyyezda < NOW()
              AND NOT EXISTS(SELECT 1 FROM otzivy WHERE bronirovaniye_id = @bid)
        )", conn);
            cmd.Parameters.AddWithValue("@bid", bronirovaniyeId);
            cmd.Parameters.AddWithValue("@uid", userId);
            return (bool)await cmd.ExecuteScalarAsync();
        }

        // получить отзывы арендатора
        public async Task<NpgsqlDataReader> GetUserReviewsAsync(int userId)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
            SELECT o.id, b.kartochka_kvartiry_id, kk.adres_kvartiry, 
                   rk.otsenka, o.tekst_otzyva, o.date, o.odobreno, o.bronirovaniye_id
            FROM otzivy o
            JOIN reyting_klassifikator rk ON rk.id = o.reyting_klassifikator_id
            JOIN bronirovaniye b ON b.id = o.bronirovaniye_id
            JOIN kartochka_kvartiry kk ON kk.id = b.kartochka_kvartiry_id
            WHERE b.arendator_polzovatel_id = @userId
            ORDER BY o.date DESC";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // Получить отзывы на квартиры арендодателя
        public async Task<NpgsqlDataReader> GetLandlordReviewsAsync(int userId)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
            SELECT o.id, kk.id, kk.adres_kvartiry, rk.otsenka, o.tekst_otzyva, o.date,
                   p.imya || ' ' || p.familiya as user_name
            FROM otzivy o
            JOIN reyting_klassifikator rk ON rk.id = o.reyting_klassifikator_id
            JOIN bronirovaniye b ON b.id = o.bronirovaniye_id
            JOIN kartochka_kvartiry kk ON kk.id = b.kartochka_kvartiry_id
            JOIN polzovatel p ON p.id = b.arendator_polzovatel_id
            WHERE kk.arendodatel_id = @userId AND o.odobreno = B'1'
            ORDER BY o.date DESC";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        // ========== ГОРОДА ==========

        // Получить все города
        public async Task<NpgsqlDataReader> GetAllCitiesAsync()
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(DatabaseQueries.GetAllCities, conn);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

    }
}
