namespace Arenda.API.Models
{
    public class CreateReviewRequest
    {
        public int BronirovaniyeId { get; set; }
        public int ReytingKlassifikatorId { get; set; }
        public string TekstOtzyva { get; set; }
    }

    public class ReviewDto
    {
        public int Id { get; set; }
        public int BronirovaniyeId { get; set; }
        public int ReytingKlassifikatorId { get; set; }
        public int Otsenka { get; set; }
        public string TekstOtzyva { get; set; }
        public DateTime Date { get; set; }
        public bool Odobreno { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserAvatar { get; set; }
        public int ApartmentId { get; set; }
        public string ApartmentName { get; set; }
    }

    public class ApartmentRatingDto
    {
        public int ApartmentId { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int Rating5 { get; set; }
        public int Rating4 { get; set; }
        public int Rating3 { get; set; }
        public int Rating2 { get; set; }
        public int Rating1 { get; set; }
    }
}
