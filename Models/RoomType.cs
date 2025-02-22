using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HotelRoomReservationSystem.Models
{
    [Table("RoomType")]
    public class RoomType
    {
        [Key, MaxLength(5)]
        public string Id { get; set; }
        [MaxLength(100), MinLength(3), Required]
        public string Name { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 10)]
        public string Description { get; set; }

        [Required]
        public int Capacity { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Precision(8, 2)]
        public decimal Price { get; set; }

        [Required, MaxLength(15)]
        public string Status { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        public ICollection<Rooms> Rooms { get; set; } // Navigation property
        
        // Navigation for RoomImages
        public List<RoomTypeImages> RoomTypeImages { get; set; }
    }
}
