using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models;

public class Memberships
{
    [Key,MaxLength(5)]
    public string Id { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required]
    public int Loyalty { get; set; } = 0;

    [Required]
    public int Points { get; set; } = 0;

    [Required, MaxLength(50)]
    public string Level { get; set; } = "Basic";

    [Required]
    public string UserId { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Active";

    public DateTime? LastCheckinDate { get; set; }

    [Required]
    public int Streak { get; set; }


    // Navigation property
    public virtual Users User { get; set; }

    // Navigation property to related MembershipRewards (One Membership can have many Rewards)
    public virtual ICollection<MembershipRewards> MembershipRewards { get; set; } = new List<MembershipRewards>();
}
