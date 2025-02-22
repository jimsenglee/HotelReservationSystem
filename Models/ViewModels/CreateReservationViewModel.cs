using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models.ViewModels
{

    public class CreateReservationViewModel
    {
        [Required(ErrorMessage = "Room Type is required.")]
        public string? RoomTypeId { get; set; }

        public string Description;

        public RoomType? RoomType { get; set; }
        public List<RoomType>? RoomTypeList { get; set; }

        public List<string>? SelectedRoomTypes { get; set; }

        public int? SelectedCapacity { get; set; }
        public List<int>? CapacityOptions { get; set; }
        public List<FeedbackWithUser>? Feedback { get; set; }
        public double? AvgRate { get; set; }

        public List<RatingPercentage>? RatingPercentages { get; set; }

        public List<RoomTypeImages>? RoomTypeImages { get; set; }

        public int FeedbackTotal { get; set; }
        [Required(ErrorMessage = "Check-in date is required.")]
        [DataType(DataType.Date)]
        [Remote("ValidateCheckInDate", "Reservation", ErrorMessage = "Check-in date cannot be in the past.")]
        public DateTime CheckInDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Check-out date is required.")]
        [DataType(DataType.Date)]
        [Remote("ValidateCheckOutDate", "Reservation", ErrorMessage = "Check-out date must be later than check-in date.")]
        public DateTime CheckOutDate { get; set; } = DateTime.Now.AddDays(1);

        public List<int>? Quantities { get; set; }  // List to store quantity of rooms per type

        public string? UsersId { get; set; }

        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters.")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? UserEmail { get; set; }


        public decimal TotalPrice { get; set; }

        public decimal TourismTax { get; set; }

        public decimal ServiceTax { get; set; }

        public decimal RoomPrice { get; set; }

        [Remote("CheckRoomAvailability", "Reservation",
                AdditionalFields = "CheckInDate,CheckOutDate",
                ErrorMessage = "The selected room is already reserved for the given dates.")]
        public string? RoomId { get; set; }

        public List<Rooms>? RoomList { get; set; }

        public Rooms? Room { get; set; }

    }
}





