using DocumentFormat.OpenXml.Spreadsheet;

namespace HotelRoomReservationSystem.Models.ViewModels
{
    public class FeedbackWithUser
    {
        public Feedback? Feedback { get; set; }
        public Users User { get; set; }

        public List<FeedbackMedia> Images { get; set; }

        public string RoomName { get; set; }
    }
}
