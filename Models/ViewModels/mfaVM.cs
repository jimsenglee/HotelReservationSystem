using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models.ViewModels;

public class mfaVM
{
    [Required(ErrorMessage = "One Time Password is Required.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Please enter a valid 6-digit number.")]
    public string oneTimePassword { get; set; }
}
