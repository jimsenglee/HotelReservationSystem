using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models.ViewModels
{
    public class RoomTypeUpdateVM
    {
        [Required(ErrorMessage = "{0} is require.")]
        [StringLength(5, ErrorMessage = "{0} must be exactly 4 characters long.", MinimumLength = 5)]
        [RegularExpression(@"^RT\d{3}$", ErrorMessage = "Invalid {0}. Format must be a letter followed by 3 digits, e.g., C001.")]
        [Remote("CheckAddIdAvailable", "RoomType", ErrorMessage = "Duplicated {0}.", AdditionalFields = nameof(Id))]
        [Display(Name = "Room Type Id")]
        public string Id { get; set; } // Example: A101, B102, C203

        [Required(ErrorMessage = "{0} is required.")]
        [MinLength(5, ErrorMessage = "{0} must not less than 5 characters.")]
        [StringLength(100, ErrorMessage = "{0} must not exceed 50 characters.")]
        [Remote("CheckNameAvailability", "RoomType", ErrorMessage = "Duplicated {0}.", AdditionalFields = nameof(Id))]
        [Display(Name = "Room Type Name")]
        public string Name { get; set; } // Example: Deluxe Room

        [Required(ErrorMessage = "{0} is required.")]
        [Range(1, 20, ErrorMessage = "{0} must be between 1 and 20.")]
        [Display(Name = "Room Capacity")]
        public int RoomCapacity { get; set; } // Example: 4 (maximum occupants)

        [Required(ErrorMessage = "{0} is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "{0} must be at least 1.")]
        [Display(Name = "Quantity")]
        public int RoomQuantity { get; set; } // Example: 10 (number of rooms available)

        [Required(ErrorMessage = "{0} is required.")]
        [Range(0.01, 10000.00, ErrorMessage = "{0} must be between 0.01 and 10,000.")]
        [DataType(DataType.Currency)]
        [Display(Name = "Price")]
        public decimal Price { get; set; } // Example: 199.99 (price per night)  

        [Required(ErrorMessage = "{0} is required.")]
        [MinLength(10, ErrorMessage = "{0} must not less than 10 characters.")]
        [StringLength(200, ErrorMessage = "{0} must not exceed 500 characters.")]
        public string Description { get; set; } // Example: A spacious room with a beautiful view.

        [Required(ErrorMessage = "At least one image is required.")]
        public List<IFormFile> Images { get; set; } // For new uploads
        public List<string> ExistingImageUrls { get; set; } = new List<string>(); // URLs of already uploaded images
        public List<string> ImagePreviews { get; set; } = new List<string>();
        public List<string> ExistingPreviews { get; set; } = new List<string>();
        public void clearImagePreviews()
        {
            ImagePreviews.Clear();
        }
    }
}
