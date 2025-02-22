namespace HotelRoomReservationSystem.Models.ViewModels
{
    public class UpdateStatusRequest
    {
        public string Id { get; set; } // For single update
        public List<string> ReservationIds { get; set; } // For bulk update
        public string Status { get; set; }
    }
}
