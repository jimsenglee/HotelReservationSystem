using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelRoomReservationSystem.Models;

public class LoginAttempt
{
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int FailedLoginAttempts { get; set; } = 0;

    [Required]
    public bool IsLocked { get; set; } = false;

  
    public DateTime? LockoutEndTime { get; set; }

    [Required]
    public string UsersId { get; set; }

    [ForeignKey(nameof(UsersId))]
    public Users Users { get; set; }
}
