namespace Arenda.API.Models
{
    public class LandlordBookingDto
    {
        public int Id { get; set; }
        public int ApartmentId { get; set; }
        public string ApartmentAddress { get; set; }
        public int TenantId { get; set; }
        public string TenantName { get; set; }
        public string TenantPhone { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserBookingDto
    {
        public int Id { get; set; }
        public int ApartmentId { get; set; }
        public string ApartmentAddress { get; set; }
        public decimal PricePerDay { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateBookingRequest
    {
        public int ApartmentId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
    }

    public class CompletedBookingDto
    {
        public int Id { get; set; }
        public int ApartmentId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public string ApartmentAddress { get; set; }
        public decimal PricePerDay { get; set; }
    }
}
