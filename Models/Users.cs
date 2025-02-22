using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HotelRoomReservationSystem.Models;


public class Users
{
    [Key, MaxLength(10)]
    public string? Id { get; set; }

    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; set; }

    [Required, DataType(DataType.EmailAddress)]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }

    [StringLength(11, MinimumLength = 10), DataType(DataType.PhoneNumber)]
    public string PhoneNum { get; set; }

    [DataType(DataType.Date)]
    public DateTime DateCreated { get; set; } = DateTime.Now;

    [DataType(DataType.Date)]
    public DateOnly DOB { get; set; }
    public string? Portrait { get; set; }


    public string? Status { get; set; }

    public string Role => GetType().Name;
}
public class Manager : Users
{

}
public class Admin : Users
{

}

// TODO
public class Customer : Users
{

}
