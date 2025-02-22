namespace HotelRoomReservationSystem.Models.ViewModels
{
    public class FeedbackDetailsVM
    {

        public string FeedbackId { get; set; }
        public string ReservationId { get; set; }
        public string UserId { get; set; }
        public string Description { get; set; }
        public double Rate { get; set; }
        public string RoomTypeName { get; set; }
        public string RoomName { get; set; }

        public List<FeedbackMedia> Images { get; set; }
    }
}
