namespace HotelRoomReservationSystem.Models.ViewModels
{
    public class RoomDetailsVM
    {
        public string RoomId { get; set; }
        public string RoomName { get; set; }
        public string RoomDescription { get; set; }

        public string Status { get; set; }
        public RoomType RoomType { get; set; } // The full Category entity
        public List<RoomTypeImages> Images { get; set; } // List of related RoomImages  
    }

}
