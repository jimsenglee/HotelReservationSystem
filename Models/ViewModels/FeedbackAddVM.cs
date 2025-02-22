using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models.ViewModels
{
    public class FeedbackAddVM
    {
        [Required]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public List<IFormFile>? Images { get; set; }

        public string? ReservationId { get; set; }
    }
}
