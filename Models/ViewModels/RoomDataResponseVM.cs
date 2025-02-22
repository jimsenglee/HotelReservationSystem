namespace HotelRoomReservationSystem.Models.ViewModels
{
    public class RoomDataResponseVM
    {
        
        public List<List<string>> RoomList { get; set; }
        public List<string> RoomRange { get; set; }
        public List<string> RoomTypeList { get; set; }
    }
}
