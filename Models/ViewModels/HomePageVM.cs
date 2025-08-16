using DocumentFormat.OpenXml.Spreadsheet;

namespace HotelRoomReservationSystem.Models.ViewModels;

public class HomePageVM
{
    public List<Feedback> FeedbackList { get; set; }
    public List<Users> UserList { get; set; }

    public List<RTWithImgVM> TypeList { get; set; }


}
