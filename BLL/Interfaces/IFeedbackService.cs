using DocumentFormat.OpenXml.Drawing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace HotelRoomReservationSystem.BLL.Interfaces
{
    public interface IFeedbackService
    {
        bool AddFeedback(Feedback feedback);
        bool RemoveFeedback(string id);

        List<Feedback> GetAllFeedbacks();

        List<Feedback> GetAllFeedbackByRoomTypId(string roomTypeId);

        List<Feedback> GetAllFeedbackById(string feedbackId);

        Feedback GetFeedbackById(string feedbackId);

        bool UpdateFeedback(Feedback feedback);
        public string GenerateCode();
        public Feedback GetFeedbackByReservationId(string reservationId);
    }
}
