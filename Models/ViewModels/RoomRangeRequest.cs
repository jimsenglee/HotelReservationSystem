using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models.ViewModels
{
    public class RoomRangeRequest
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(4, ErrorMessage = "{0} must be exactly 4 characters long.", MinimumLength = 4)]
        [RegularExpression(@"^[A-Z]\d{3}$", ErrorMessage = "Invalid {0}. Format must be a letter followed by 3 digits, e.g., A101.")]
        public string StartRoom { get; set; }

        [Required(ErrorMessage = "End Room is required.")]
        [RegularExpression(@"^[A-Z]\d{3}$", ErrorMessage = "Invalid End Room format.")]
        [Remote("CheckValueValidateEndRoom", "RoomType", ErrorMessage = "End Room validation failed.", AdditionalFields = nameof(StartRoom))]
        public string EndRoom { get; set; }
    }
}
