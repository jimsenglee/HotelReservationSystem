using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models.ViewModels;

public class ContactMessageVM
{
    [Required]
    [MaxLength(20,ErrorMessage = "Name length should be within 20 character.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Phone Number is required.")]
    [RegularExpression(@"^(\+60|0)(11\d{8}|1[02-9]\d{7}|[2-9]\d{7})$",
     ErrorMessage = "Invalid phone number format.")]
    public string Phone { get; set; }

    [Required]
    [EmailAddress(ErrorMessage = "Invalid Email Address Format.")]
    public string Email { get; set; }

    [Required]
    [MaxLength(100, ErrorMessage = "Message length should be within 200 character.")]
    public string Message { get; set; }
}
