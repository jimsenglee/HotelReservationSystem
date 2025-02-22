using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models
{
    public class Reservation
    {
        [Key, MaxLength(15)]
        public string Id { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Required, Precision(10, 2)]
        public decimal TotalPrice { get; set; }

        [Required, MaxLength(15)]
        public string Status { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        // For guest user, store the name here.
        public string? UserName { get; set; } // Nullable for cases where User is a registered user

        // This can still link to the User table for registered users.
        public string? UsersId { get; set; }

        public string UserEmail { get; set; }

        [Required]
        public string RoomId { get; set; }

        // Navigation Properties
        public Users Users { get; set; }
        public Rooms Room { get; set; }
    }


}
