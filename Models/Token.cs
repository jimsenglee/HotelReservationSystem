using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelRoomReservationSystem.Models;

public class Token
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string token { get; set; }

    [Required]
    public DateTime Expiration { get; set; }

    [Required]
    public string? Purpose { get; set; }

    [Required]
    public string UsersId { get; set; }



    [ForeignKey(nameof(UsersId))]
    public Users Users { get; set; }
}
