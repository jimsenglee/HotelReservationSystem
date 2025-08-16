using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models.ViewModels;

public class UserDetailVM
{
    public string? Id { get; set; }

    public string Name { get; set; }

    public string Email { get; set; }

    public string PhoneNum { get; set; }

    public DateTime DateCreated { get; set; }

    public DateOnly DOB { get; set; }
    public string Portrait { get; set; }

    public string? Status { get; set; }

    public string Role { get; set; }
}
