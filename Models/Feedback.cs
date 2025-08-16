using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace HotelRoomReservationSystem.Models
{
    [Table("Feedback")]

    public class Feedback
    {
        [Key]
        [MinLength(11)]
        [MaxLength(11)]
        public string Id { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Required]
        [Range(0.5, 5)]
        public double Rating { get; set; }

        [Required]
        public string ReservationId { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;
        [Required]
        public string UserId { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual Users Users { get; set; }

        [ForeignKey(nameof(ReservationId))]
        public virtual Reservation Reservation { get; set; }
    }
}
