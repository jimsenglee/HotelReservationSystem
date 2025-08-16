using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace HotelRoomReservationSystem.Models
{
    [Table("WaitingList")]
    public class WaitingList
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string RoomId { get; set; }

        [Required]
        public string OrgRoomTypeId { get; set; }

        public string? NewRoomTypeId { get; set; }

        [Required]
        public string Action { get; set; } // Replace or Disabled

        [Required]
        public DateTime DatePerform { get; set; }

        [ForeignKey(nameof(OrgRoomTypeId))]
        public RoomType RoomType { get; set; }
    }
}
