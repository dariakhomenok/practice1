using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arenda.DBeaver
{
    public class DatabaseQueries
    {
        public const string GetUserByLogin = @"
            SELECT id, login, parol, email, telefon, imya, familiya
            FROM polzovatel
            WHERE login = @login OR email = @email;";

        public const string CreateUser = @"
            INSERT INTO polzovatel (login, parol, email, telefon, imya, familiya)
            VALUES (@login, @parol, @email, @telefon, @imya, @familiya)
            RETURNING id;";

        public const string GetUserById = @"
        SELECT id, login, parol, email, telefon, imya, familiya, data_rozhdeniya, '' as photo
        FROM polzovatel 
        WHERE id = @id;";

        public const string UpdateUser = @"
        UPDATE polzovatel
        SET email = @email, 
            telefon = @telefon, 
            imya = @imya, 
            familiya = @familiya,
            data_rozhdeniya = @data_rozhdeniya
        WHERE id = @id";

        // ========== АВАТАРЫ ==========
        public const string GetUserAvatar = @"
        SELECT url_avatar FROM avatary_polzovatelya 
        WHERE polzovatel_id = @userId;";

        public const string UpdateUserAvatar = @"
        INSERT INTO avatary_polzovatelya (polzovatel_id, url_avatar, imya_faila, data_obnovleniya)
        VALUES (@userId, @photoUrl, @fileName, NOW())
        ON CONFLICT (polzovatel_id) 
        DO UPDATE SET url_avatar = @photoUrl, imya_faila = @fileName, data_obnovleniya = NOW();";

        public const string DeleteUserAvatar = @"
        DELETE FROM avatary_polzovatelya WHERE polzovatel_id = @userId;";

        // ========== КВАРТИРЫ ==========
        public const string GetAllApartments = @"
            SELECT
                kk.id,
                kk.adres_kvartiry,
                kk.tsena,
                kk.ploshad,         
                kk.komnat,
                g.nazvanie_goroda AS gorod
            FROM kartochka_kvartiry kk
            JOIN gorod g ON kk.gorod_id = g.id
            ORDER BY kk.id DESC;";

        public const string GetApartmentById = @"
            SELECT * FROM kartochka_kvartiry WHERE id = @id;";

        public const string GetApartmentsByLandlord = @"
            SELECT
                kk.*,
                g.nazvanie_goroda AS CityName
            FROM kartochka_kvartiry kk
            JOIN gorod g ON kk.gorod_id = g.id
            WHERE kk.arendodatel_id = @userId
            ORDER BY kk.id DESC;";

        public const string CreateApartment = @"
            INSERT INTO kartochka_kvartiry (
                arendodatel_id, gorod_id, opisaniye_kvartiry, adres_kvartiry,
                ploshad, tsena, komnat, etazh, etazhnost, remont,
                mebel, tehnika, internet, parkovka, lift, balkon,
                zhivotnye_dopustimo, deti_dopustimy, kuriye_dopustimo,
                min_srok_v_sutkah, zalog
            ) VALUES (
                @arendodatel_id, @gorod_id, @opisanie, @adres,
                @ploschad, @tsena, @komnat, @etazh, @etazhnost, @remont,
                @mebel, @tehnika, @internet, @parkovka, @lift, @balkon,
                @zhivotnye_dopustimy, @deti_dopustimy, @kuriye_dopustimo,
                @min_srok, @zalog
            ) RETURNING id;";

        public const string UpdateApartment = @"
            UPDATE kartochka_kvartiry SET
                gorod_id = @gorod_id,
                opisaniye_kvartiry = @opisanie,
                adres_kvartiry = @adres,
                ploshad = @ploschad,
                tsena = @tsena,
                komnat = @komnat,
                etazh = @etazh,
                etazhnost = @etazhnost,
                mebel = @mebel,
                tehnika = @tehnika,
                internet = @internet,
                parkovka = @parkovka,
                lift = @lift,
                balkon = @balkon,
                zhivotnye_dopustimo = @zhivotnye_dopustimy,
                deti_dopustimy = @deti_dopustimy,
                kuriye_dopustimo = @kuriye_dopustimo,
                min_srok_v_sutkah = @min_srok,
                zalog = @zalog
            WHERE id = @id
            RETURNING id;";

        public const string DeleteApartment = @"
            DELETE FROM kartochka_kvartiry WHERE id = @id;";

        // ========== БРОНИРОВАНИЯ ==========
        public const string CheckAvailability = @"
            SELECT COUNT(*) FROM bronirovaniye
            WHERE kartochka_kvartiry_id = @apartment_id
            AND data_zayezda < @check_out
            AND data_vyyezda > @check_in
            AND status != 'Отменена';";

        public const string CreateBooking = @"
            INSERT INTO bronirovaniye (
                arendator_polzovatel_id, kartochka_kvartiry_id,
                data_zayezda, data_vyyezda, kolvo_vzroslih, kolvo_detey
            ) VALUES (
                @user_id, @apartment_id, @check_in, @check_out, @adults, @children
            ) RETURNING id;";

        public const string GetUserBookings = @"
            SELECT
                b.*,
                kk.adres_kvartiry,
                kk.tsena
            FROM bronirovaniye b
            JOIN kartochka_kvartiry kk ON b.kartochka_kvartiry_id = kk.id
            WHERE b.arendator_polzovatel_id = @user_id
            ORDER BY b.sozdano DESC;";

        public const string GetLandlordBookings = @"
            SELECT
                b.*,
                kk.adres_kvartiry,
                p.imya || ' ' || p.familiya AS arendator_name
            FROM bronirovaniye b
            JOIN kartochka_kvartiry kk ON b.kartochka_kvartiry_id = kk.id
            JOIN polzovatel p ON b.arendator_polzovatel_id = p.id
            WHERE kk.arendodatel_id = @user_id
            ORDER BY b.sozdano DESC;";

        public const string CancelBooking = @"
            UPDATE bronirovaniye SET status = 'Отменена'
            WHERE id = @id AND status = 'Новая'
            RETURNING id;";

        // ========== СТАТИСТИКА ==========
        public const string GetLandlordApartmentsCount = @"
            SELECT COUNT(*) FROM kartochka_kvartiry WHERE arendodatel_id = @user_id;";

        public const string GetLandlordActiveBookingsCount = @"
            SELECT COUNT(*)
            FROM bronirovaniye b
            JOIN kartochka_kvartiry kk ON b.kartochka_kvartiry_id = kk.id
            WHERE kk.arendodatel_id = @user_id
            AND b.data_vyyezda >= CURRENT_DATE
            AND b.status = 'Новая';";

        public const string GetLandlordMonthlyIncome = @"
            SELECT COALESCE(SUM(kk.tsena * (b.data_vyyezda - b.data_zayezda)), 0) AS income
            FROM bronirovaniye b
            JOIN kartochka_kvartiry kk ON b.kartochka_kvartiry_id = kk.id
            WHERE kk.arendodatel_id = @user_id
            AND EXTRACT(MONTH FROM b.data_zayezda) = EXTRACT(MONTH FROM CURRENT_DATE)
            AND b.status != 'Отменена';";

        // ========== ПОИСК ==========
        public const string SearchApartments = @"
            SELECT
                kk.*,
                g.nazvanie_goroda AS CityName
            FROM kartochka_kvartiry kk
            JOIN gorod g ON kk.gorod_id = g.id
            WHERE
                (g.id = @city_id OR @city_id IS NULL)
                AND kk.tsena BETWEEN @min_price AND @max_price
                AND kk.komnat >= @rooms
                AND NOT EXISTS (
                    SELECT 1 FROM bronirovaniye b
                    WHERE b.kartochka_kvartiry_id = kk.id
                    AND b.data_zayezda < @check_out
                    AND b.data_vyyezda > @check_in
                    AND b.status != 'Отменена'
                )
            ORDER BY kk.tsena;";

        // ========== ГОРОДА ==========
        public const string GetAllCities = @"
            SELECT g.id, g.nazvanie_goroda as name
            FROM gorod g
            ORDER BY g.nazvanie_goroda;
        ";
    }
}
