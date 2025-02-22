using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models.ViewModels
{
    public class RoomsUpdateVM
    {
        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(4, ErrorMessage = "{0} must be exactly 4 characters long.", MinimumLength = 4)]
        [RegularExpression(@"^[A-Z]\d{3}$", ErrorMessage = "Invalid {0}. Format must be a letter followed by 3 digits, e.g., A101.")]
        [Remote("CheckIdAvailable", "Rooms", ErrorMessage = "Duplicated {0}.", AdditionalFields = nameof(Id))]
        [Display(Name = "Room Id")]
        public string Id { get; set; } // Room ID should remain required and unique.

        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(4, ErrorMessage = "{0} must be exactly 4 characters long.", MinimumLength = 4)]
        [RegularExpression(@"^[A-Z]\d{3}$", ErrorMessage = "Invalid {0}. Format must be a letter followed by 3 digits, e.g., A101.")]
        [Remote("CheckNameAvailable", "Rooms", ErrorMessage = "Duplicated {0}.", AdditionalFields = nameof(Id))]
        [Display(Name = "Room Number")]
        public string Name { get; set; } // Example: Deluxe Room

        [Required(ErrorMessage = "{0} is required.")]
        [MinLength(5, ErrorMessage = "{0} must not be less than 5 characters.")]
        [StringLength(50, ErrorMessage = "{0} must not exceed 50 characters.")]
        [Display(Name = "Room Type")]
        public string RoomTypeName { get; set; } // Example: Deluxe

        [Required(ErrorMessage = "{0} is required.")]
        [MinLength(10, ErrorMessage = "{0} must not be less than 10 characters.")]
        [StringLength(200, ErrorMessage = "{0} must not exceed 500 characters.")]
        public string Description { get; set; } // Example: A spacious room with a beautiful view.

        [Required(ErrorMessage = "{0} is required.")]
        [Range(1, 20, ErrorMessage = "{0} must be between 1 and 20.")]
        [Display(Name = "Room Capacity")]
        public int RoomCapacity { get; set; } // Example: 4 (maximum occupants)

        [Required(ErrorMessage = "{0} is required.")]
        [Range(0.01, 10000.00, ErrorMessage = "{0} must be between 0.01 and 10,000.")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; } // Example: 199.99 (price per night)

        // For update, existing images can be retained without uploading new ones
        public List<IFormFile> Images { get; set; } = new List<IFormFile>(); // Optional new uploads.

        public List<string> ExistingImageUrls { get; set; } = new List<string>(); // URLs of already uploaded images

        public List<string> ImagePreviews { get; set; } = new List<string>();

        [Display(Name = "Existing Previews")]
        public List<string> ExistingPreviews { get; set; } = new List<string>();
    }
}
