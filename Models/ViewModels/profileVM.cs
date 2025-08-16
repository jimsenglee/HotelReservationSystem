using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace HotelRoomReservationSystem.Models.ViewModels;

#nullable disable warnings

public class profileVM
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required.")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Birth date is required.")]
    [DataType(DataType.Date, ErrorMessage = "Invalid date format.")]
    [Remote("CheckBirthDay", "Account", ErrorMessage = "Age Must be withn 18 - 110.")]
    public DateOnly BirthDay { get; set; }

    public DateTime? CreatedAt { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Phone Number is required.")]
    [RegularExpression(@"^(\+60|0)(11\d{8}|1[02-9]\d{7}|[2-9]\d{7})$",
     ErrorMessage = "Invalid phone number format.")]
    public string PhoneNum { get; set; }

    public string? Portrait { get; set; }

    public IFormFile? Photo { get; set; }

    public string? Base64Photo { get; set; }

    //public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    //{
    //    if (Photo == null && string.IsNullOrEmpty(Base64Photo))
    //    {
    //        yield return new ValidationResult("Either upload a photo or capture one using the webcam.", new[] { nameof(Photo), nameof(Base64Photo) });
    //    }
    //}
    public Memberships? Memberships { get; set; }

    public virtual ICollection<MembershipRewards>? MembershipRewards { get; set; } = new List<MembershipRewards>();

    public virtual ICollection<Rewards>? Rewards { get; set; } = new List<Rewards>();
}