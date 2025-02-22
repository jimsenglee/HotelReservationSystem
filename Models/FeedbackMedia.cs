using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelRoomReservationSystem.Models
{
    [Table("FeedbackMedia")]

    public class FeedbackMedia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string FileType { get; set; }

        // Foreign Key for Rooms (if RoomImages are tied to Rooms)
        [Required, MaxLength(11)] // F2412260001
        public string FeedbackId { get; set; }

        // Navigation Property
        [ForeignKey(nameof(FeedbackId))]
        public Feedback Feedback{ get; set; }
    }
}
