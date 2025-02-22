namespace HotelRoomReservationSystem.Models.ViewModels
{

    public class TransactionForProfileVM
    {
        public Models.Transaction Transaction { get; set; }
        public bool IsRated { get; set; }
        public string RoomTypeImage { get; set; } // New property for the room type image }

    }
}