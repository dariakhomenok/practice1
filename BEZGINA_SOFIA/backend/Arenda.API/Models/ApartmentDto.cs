using System.ComponentModel.DataAnnotations;

namespace Arenda.API.Models
{
    // ========== ДЛЯ СПИСКА КВАРТИР ==========
    public class ApartmentListItemDto
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public decimal Price { get; set; }
        public int Area { get; set; }
        public int Rooms { get; set; }
        public string CityName { get; set; }
        public string MainPhoto { get; set; }
        public string LandlordName { get; set; }
    }

    // ========== ДЛЯ ДЕТАЛЬНОЙ СТРАНИЦЫ КВАРТИРЫ ==========
    public class ApartmentDetailDto
    {
        public int Id { get; set; }
        public int LandlordId { get; set; }
        public string LandlordName { get; set; }
        public string LandlordPhone { get; set; }
        public string LandlordEmail { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public int Area { get; set; }
        public decimal Price { get; set; }
        public int Rooms { get; set; }
        public int? Floor { get; set; }
        public int? TotalFloors { get; set; }
        public string Renovation { get; set; }           // ← новое поле
        public bool Furniture { get; set; }
        public bool Appliances { get; set; }
        public bool Internet { get; set; }
        public bool Parking { get; set; }
        public bool Elevator { get; set; }
        public bool Balcony { get; set; }
        public bool PetsAllowed { get; set; }
        public bool ChildrenAllowed { get; set; }
        public bool SmokingAllowed { get; set; }
        public int MinRentDays { get; set; }
        public decimal? Deposit { get; set; }
        public List<PhotoDto> Photos { get; set; }
    }

    // ========== ДЛЯ ФОТОГРАФИЙ ==========
    public class PhotoDto
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public int Order { get; set; }
    }

    // ========== ЗАПРОС НА СОЗДАНИЕ КВАРТИРЫ ==========
    public class CreateApartmentRequest
    {
        [Required]
        public int CityId { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [StringLength(255)]
        public string Address { get; set; }

        [Required]
        public int Area { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int Rooms { get; set; }

        public int? Floor { get; set; }
        public int? TotalFloors { get; set; }
        public string? Renovation { get; set; }           // ← новое поле

        public bool Furniture { get; set; }
        public bool Appliances { get; set; }
        public bool Internet { get; set; }
        public bool Parking { get; set; }
        public bool Elevator { get; set; }
        public bool Balcony { get; set; }
        public bool PetsAllowed { get; set; }
        public bool ChildrenAllowed { get; set; }
        public bool SmokingAllowed { get; set; }

        [Required]
        public int MinRentDays { get; set; }

        public decimal? Deposit { get; set; }
    }

    // ========== ЗАПРОС НА ОБНОВЛЕНИЕ КВАРТИРЫ ==========
    public class UpdateApartmentRequest : CreateApartmentRequest
    {
    }

    // ========== ЗАПРОС НА ПОИСК КВАРТИР ==========
    public class SearchApartmentsRequest
    {
        // Цена
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        // Залог
        public decimal? MinDeposit { get; set; }
        public decimal? MaxDeposit { get; set; }

        // Комнаты: 0-студия, 1-1 комната, 2-2 комнаты, 3-3 комнаты, 4-4+ комнаты
        public int? Rooms { get; set; }

        // Срок аренды (в сутках)
        public int? MinRentDays { get; set; }
        public int? MaxRentDays { get; set; }

        // Удобства
        public bool? Furniture { get; set; }
        public bool? Appliances { get; set; }
        public bool? Internet { get; set; }
        public bool? Parking { get; set; }
        public bool? Elevator { get; set; }
        public bool? Balcony { get; set; }

        // Правила проживания
        public bool? PetsAllowed { get; set; }
        public bool? ChildrenAllowed { get; set; }
        public bool? SmokingAllowed { get; set; }

        // Ремонт
        public string? Renovation { get; set; }

        // Площадь
        public int? MinArea { get; set; }
        public int? MaxArea { get; set; }

        // Этаж
        public int? MinFloor { get; set; }
        public int? MaxFloor { get; set; }
        public bool? NotFirstFloor { get; set; }
        public bool? NotLastFloor { get; set; }

        // Расположение
        public int? CityId { get; set; }
        public string? AddressSearch { get; set; }

        // Пагинация
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
