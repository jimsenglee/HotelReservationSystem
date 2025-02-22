using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace HotelRoomReservationSystem.Models.ViewModels;

public class UpdateRewardsVM
{
    [Required(ErrorMessage = "{0} is require.")]
    [StringLength(4, ErrorMessage = "{0} must be exactly 4 characters long.", MinimumLength = 4)]
    [RegularExpression(@"^[A-Z]\d{3}$", ErrorMessage = "Invalid {0}. Format must be a letter followed by 3 digits, e.g., A101.")]
    //[Remote("CheckIdAvailability", "Rewards", ErrorMessage = "Duplicated {0}.")]
    [Display(Name = "Room Id")]
    public string Id { get; set; } // Unique Reward ID

    [Required(ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> {0} is required.")]
    [MinLength(5, ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> {0} must not less than 5 characters.")]
    [StringLength(100, ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> {0} must not exceed 100 characters.")]
    //[Remote("CheckNameAvailability", "Rewards", ErrorMessage = "Duplicated {0}.")]
    [Display(Name = "Room Name")]
    public string Name { get; set; } // Reward name (e.g., "Free Breakfast")

    [Required(ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> {0} is required.")]
    [StringLength(500, ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> {0} must not exceed 500 characters.")]
    [Display(Name = "Description")]
    public string Description { get; set; } // Detailed description of the reward

    [Required(ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> {0} is required.")]
    [Display(Name = "Status")]
    public string Status { get; set; } // Status of reward (e.g., "Active", "Inactive")
     
    [Required(ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> {0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> {0} must be at least 1.")]
    [Display(Name = "Points Required")]
    public int PointsRequired { get; set; } // Points needed to redeem the reward

    [Required(ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> {0} is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Valid From")]
    public DateTime ValidFrom { get; set; } // Start date of reward validity

    [DataType(DataType.Date)]
    [Display(Name = "Valid Until")]
    public DateTime? ValidUntil { get; set; } // End date of reward validity (nullable if no expiration)

    [Required(ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> {0} is required.")]
    [StringLength(10, ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> {0} must not exceed 10 characters.")]
    //[RegularExpression(@"^[A-Z]{3}-\d{4}$", ErrorMessage = "Invalid {0}. Format must be three uppercase letters followed by a hyphen and four digits (e.g., ABC-1234).")]
    //[Remote("CheckRewardCodeAvailability", "Rewards", ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> Duplicated {0}.")]
    [Display(Name = "Reward Code")]
    public string RewardCode { get; set; }


    [Required(ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> {0} is required.")]
    [Display(Name = "Quantity")]
    public int Quantity { get; set; } // Quantity of rewards available

    [Required(ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> {0} is required.")]
    [Range(1, 100, ErrorMessage = "<i class=\"fa-solid fa-triangle-exclamation\"></i> {0} must be at least 1 and less than or equal to 100.")]
    [Display(Name = "DiscountRate")]
    public decimal DiscountRate { get; set; } // Quantity of rewards available

}
