using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models.ViewModels;

#nullable disable warnings

public class RegistrationVM
{

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(12, ErrorMessage = "First name cannot be more than 12 characters.")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(12, ErrorMessage = "Last name cannot be more than 12 characters.")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Birth date is required.")]
    [DataType(DataType.Date, ErrorMessage = "Invalid date format.")]
    [Remote("CheckBirthDay", "Account", ErrorMessage = "Age Must be withn 18 - 110.")]
    public DateOnly BirthDay { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Phone Number is required.")]
    [RegularExpression(@"^(\+60|0)(11\d{8}|1[02-9]\d{7}|[2-9]\d{7})$",
     ErrorMessage = "Invalid phone number format.")]
    public string PhoneNum { get; set; }

    [Required(ErrorMessage = "Recaptcha is required.")]
    public string? RecaptchaToken { get; set; } 

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$", ErrorMessage = "Password must contain at least one letter and one number.")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Password confirmation is required.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string Password2 { get; set; }
}
