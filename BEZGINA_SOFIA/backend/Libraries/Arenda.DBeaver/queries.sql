-- ==========================================
-- ПОЛЬЗОВАТЕЛИ
-- ==========================================

-- 1. Получить пользователя по логину или email (для входа)
SELECT id, login, parol, email, telefon, imya, familiya
FROM polzovatel
WHERE login = @login OR email = @email;

-- 2. Создать нового пользователя
INSERT INTO polzovatel (login, parol, email, telefon, imya, familiya)
OUTPUT INSERTED.id
VALUES (@login, @parol, @email, @telefon, @imya, @familiya)

-- 3. Получить пользователя по ID
SELECT * FROM polzovatel WHERE id = @id;

-- 4. Обновить данные пользователя
UPDATE polzovatel
SET email = @email, telefon = @telefon, imya = @imya, familiya = @familiya
OUTPUT INSERTED.id
WHERE id = @id

-- ==========================================
-- КВАРТИРЫ
-- ==========================================

-- 5. Получить все квартиры (для списка)
SELECT
    kk.id,
    kk.adres_kvartiry,
    kk.tsena,
    kk.ploschad,
    kk.komnat,
    g.nazvanie_goroda AS gorod
FROM kartochka_kvartiry kk
JOIN gorod g ON kk.gorod_id = g.id
ORDER BY kk.id DESC;

-- 6. Получить квартиру по ID
SELECT * FROM kartochka_kvartiry WHERE id = @id;

-- 7. Получить квартиры конкретного арендодателя
SELECT
    kk.*,
    g.nazvanie_goroda
FROM kartochka_kvartiry kk
JOIN gorod g ON kk.gorod_id = g.id
WHERE kk.arendodatel_id = @userId
ORDER BY kk.id DESC;

-- 8. Добавить квартиру
INSERT INTO kartochka_kvartiry (
    arendodatel_id, gorod_id, opisaniye_kvartiry, adres_kvartiry,
    ploschad, tsena, komnat, etazh, etazhnost, mebel, tehnika,
    internet, parkovka, lift, balkon, zhivotnye_dopustimy,
    deti_dopustimy, kuriye_dopustimo, min_srok_v_sutkah, zalog
) 
OUTPUT INSERTED.id
VALUES (
    @arendodatel_id, @gorod_id, @opisanie, @adres,
    @ploschad, @tsena, @komnat, @etazh, @etazhnost, @mebel, @tehnika,
    @internet, @parkovka, @lift, @balkon, @zhivotnye_dopustimy,
    @deti_dopustimy, @kuriye_dopustimo, @min_srok, @zalog
)

-- 9. Обновить квартиру
UPDATE kartochka_kvartiry SET
    gorod_id = @gorod_id,
    opisaniye_kvartiry = @opisanie,
    adres_kvartiry = @adres,
    ploschad = @ploschad,
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
    zhivotnye_dopustimy = @zhivotnye_dopustimy,
    deti_dopustimy = @deti_dopustimy,
    kuriye_dopustimo = @kuriye_dopustimo,
    min_srok_v_sutkah = @min_srok,
    zalog = @zalog
OUTPUT INSERTED.id
WHERE id = @id

-- 10. Удалить квартиру
DELETE FROM kartochka_kvartiry WHERE id = @id;

-- ==========================================
-- БРОНИРОВАНИЯ
-- ==========================================

-- 11. Проверить доступность квартиры на даты
SELECT COUNT(*) FROM bronirovaniye
WHERE kartochka_kvartiry_id = @apartment_id
AND data_zayezda < @check_out
AND data_vyyezda > @check_in
AND status != 'Отменена';

-- 12. Создать бронь
INSERT INTO bronirovaniye (
    arendator_polzovatel_id, kartochka_kvartiry_id,
    data_zayezda, data_vyyezda, kolvo_vzroslih, kolvo_detey
) 
OUTPUT INSERTED.id
VALUES (
    @user_id, @apartment_id, @check_in, @check_out, @adults, @children
) 

-- 13. Получить брони пользователя (арендатор)
SELECT
    b.*,
    kk.adres_kvartiry,
    kk.tsena
FROM bronirovaniye b
JOIN kartochka_kvartiry kk ON b.kartochka_kvartiry_id = kk.id
WHERE b.arendator_polzovatel_id = @user_id
ORDER BY b.sozdano DESC;

-- 14. Получить брони квартир арендодателя
SELECT
    b.*,
    kk.adres_kvartiry,
    p.imya + ' ' + p.familiya AS arendator_name
FROM bronirovaniye b
JOIN kartochka_kvartiry kk ON b.kartochka_kvartiry_id = kk.id
JOIN polzovatel p ON b.arendator_polzovatel_id = p.id
WHERE kk.arendodatel_id = @user_id
ORDER BY b.sozdano DESC;

-- 15. Отменить бронь
UPDATE bronirovaniye SET status = 'Отменена'
OUTPUT INSERTED.id
WHERE id = @id AND status = 'Новая'

-- 16. Получить конкретную бронь
SELECT
    b.*,
    kk.adres_kvartiry,
    kk.tsena,
    p.telefon AS arendator_phone
FROM bronirovaniye b
JOIN kartochka_kvartiry kk ON b.kartochka_kvartiry_id = kk.id
JOIN polzovatel p ON b.arendator_polzovatel_id = p.id
WHERE b.id = @id;

-- ==========================================
-- СТАТИСТИКА
-- ==========================================

-- 17. Количество квартир арендодателя
SELECT COUNT(*) FROM kartochka_kvartiry WHERE arendodatel_id = @user_id;

-- 18. Количество активных броней арендодателя
SELECT COUNT(*)
FROM bronirovaniye b
JOIN kartochka_kvartiry kk ON b.kartochka_kvartiry_id = kk.id
WHERE kk.arendodatel_id = @user_id
AND b.data_vyyezda >= GETDATE()
AND b.status = 'Новая';

-- 19. Доход арендодателя за месяц
SELECT COALESCE(SUM(kk.tsena * (b.data_vyyezda - b.data_zayezda)), 0) AS income
FROM bronirovaniye b
JOIN kartochka_kvartiry kk ON b.kartochka_kvartiry_id = kk.id
WHERE kk.arendodatel_id = @user_id
AND YEAR(b.data_zayezda) = YEAR(GETDATE())
AND b.status != 'Отменена';

-- ==========================================
-- ПОИСК С ФИЛЬТРАМИ
-- ==========================================

-- 20. Поиск квартир с фильтрами
SELECT
    kk.*,
    g.nazvanie_goroda
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
ORDER BY kk.tsena;