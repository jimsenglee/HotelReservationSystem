using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HotelRoomReservationSystem.Models;

[Table("MembershipRewards")]
[PrimaryKey(nameof(MembershipId), nameof(RewardId))]
public class MembershipRewards
{
    [MaxLength(5)]
    public string MembershipId { get; set; }

    [MaxLength(10)]
    public string RewardId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1; // Default quantity to 1

    // Navigation properties
    [ForeignKey("MembershipId")]
    public virtual Memberships Membership { get; set; }

    [ForeignKey("RewardId")]
    public virtual Rewards Reward { get; set; }

}


