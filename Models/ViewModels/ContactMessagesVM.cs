using System.ComponentModel.DataAnnotations;

namespace HotelRoomReservationSystem.Models.ViewModels;

public class ContactMessagesVM
{

   
    public string Id { get; set; }

   
    public string Name { get; set; }

  
    public string Phone { get; set; }

   
    public string Email { get; set; }

  
    public string Messages { get; set; }

    public string ReplyMessage { get; set; }


    public string Status { get; set; } 

    public DateTime CreatedDate { get; set; } 

    public DateTime? ResolvedDate { get; set; }
}
