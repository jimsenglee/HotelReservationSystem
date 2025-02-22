using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HotelRoomReservationSystem.Models
{
    public class Transaction
    {
        [Key, MaxLength(10)]
        public string Id { get; set; }

        [Required, StringLength(50, MinimumLength = 3)]
        public string PaymentMethod { get; set; }

        [Required, Precision(10, 2)]
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PaymentDate { get; set; }

        [Required, MaxLength(15)]
        public string Status { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        
        public string? UsersId { get; set; }

        // Navigation Property
        public Users Users { get; set; }

        [Required]
        public string ReservationId { get; set; }

        // Navigation Property
        public Reservation Reservation { get; set; }
    }

}
