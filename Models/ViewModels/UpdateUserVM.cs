using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models.ViewModels;

public class UpdateUserVM
{
    public string? id { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(12, ErrorMessage = "First name cannot be more than 12 characters.")] 
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

    public string? portrait { get; set; }

    public IFormFile? Photo { get; set; }

    public string? Base64Photo { get; set; }
}
