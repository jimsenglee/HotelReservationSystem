using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models
{
    [Table("RoomTypeImages")]

    public class RoomTypeImages
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name{ get; set; }

        [Required]
        public string FileType { get; set; }

        // Foreign Key for Rooms (if RoomImages are tied to Rooms)
        [Required, MaxLength(5)]
        public string RoomTypeId { get; set; }

        // Navigation Property
        [ForeignKey(nameof(RoomTypeId))]
        public RoomType RoomType { get; set; }
    }
}
