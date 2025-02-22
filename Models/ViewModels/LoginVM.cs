using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models.ViewModels;

public class LoginVM
{
    [Required(ErrorMessage = "email is required.")]
    public string email { get; set; }

    [Required(ErrorMessage = "password is required.")]
    public string password { get; set; }

    public bool RememberMe { get; set; }
}
