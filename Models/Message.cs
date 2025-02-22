using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models
{
    public class Message
    {
        [Key]
        public string Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        [MaxLength(11)]
        public string Phone { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Messages { get; set; }

     
        public string ReplyMessage { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Unread";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedDate { get; set; }
    }

}

