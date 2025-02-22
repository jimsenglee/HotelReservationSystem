using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models
{
    public class MFALogin
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string status { get; set; }

        [Required]

        public string? otp { get; set; }

        [Required]
        public DateTime? ExipredDateTime { get; set; }


        [Required]
        public string UsersId { get; set; }

        [ForeignKey(nameof(UsersId))]
        public Users Users { get; set; }
    }
}
