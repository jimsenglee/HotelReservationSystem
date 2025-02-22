using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;

namespace HotelRoomReservationSystem.Models;

[Table("Rewards")]
public class Rewards
{
    [Key, MaxLength(10)]  // Ensure length matches the RoomId in RoomImages
    public string Id { get; set; } // Unique Reward ID

    [MaxLength(35)]
    [Required]
    public string Name { get; set; } // Reward name (e.g., "Free Breakfast")

    [StringLength(200, MinimumLength = 10)]
    [Required]
    public string Description { get; set; } // Detailed description of the reward

    [Required]
    [MaxLength(15)]
    public string Status { get; set; } // Status of reward (e.g., "Active", "Inactive")

    [Required]
    [Range(0, int.MaxValue)]
    public int PointsRequired { get; set; } // Points needed to redeem the reward

    [Required]
    public DateTime ValidFrom { get; set; } // Start date of reward validity

    public DateTime? ValidUntil { get; set; } // End date of reward validity (nullable if no expiration)

    [Required]
    [MaxLength(10)]
    public string RewardCode { get; set; } 

    [Required]
    [Range(0, int.MaxValue)]
    public int Quantity { get; set; } // Quantity of rewards available

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountRate { get; set; } // Quantity of rewards available
}
