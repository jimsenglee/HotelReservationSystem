using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelRoomReservationSystem.Models
{
    [Table("Rooms")]

    public class Rooms
    {
        // Column
        [Key, MaxLength(5)]  // Ensure length matches the RoomId in RoomImages
        public string Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(15)]
        public string Status { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        // Foreign Key
        [Required]
        [MaxLength(5)]  // Ensure this matches the length in CategoryId
        public string RoomTypeId { get; set; }

        // Navigation
        [ForeignKey(nameof(RoomTypeId))]
        public RoomType RoomType { get; set; }
    }

}