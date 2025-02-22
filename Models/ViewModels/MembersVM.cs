using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace HotelRoomReservationSystem.Models.ViewModels;

public class MembersVM 
{
    [Display(Name = "Member Id")]
    public string Id { get; set; }

    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [Display(Name = "Loyalty")]
    public int Loyalty { get; set; }

    [Display(Name = "Points")]
    public int Points { get; set; }

    [Display(Name = "Member Level")]
    public string Level { get; set; }

    [Display(Name = "User Id")]
    public string UserId { get; set; }

    [Display(Name = "Status")]
    public string Status { get; set; }

    [Display(Name = "Last Check In Date")]
    public DateTime? LastCheckinDate { get; set; }

    [Display(Name = "Streak")]
    public int Streak { get; set; }

    public string UserName { get; set; }

}
