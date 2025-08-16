using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.DAL.Interfaces;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using NuGet.Protocol.Core.Types;

namespace HotelRoomReservationSystem.BLL
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository feedbackRepository;
        public FeedbackService(IFeedbackRepository feedbackRepository)
        {
            this.feedbackRepository = feedbackRepository;
        }

        public bool AddFeedback(Feedback feedback)
        {
            if (feedback != null)
                if (feedbackRepository.Add(feedback) > 0) return true;
            return false;
        }

        public List<Feedback> GetAllFeedbackById(string feedbackId)
        {
            return feedbackRepository.GetAllByRTId(feedbackId);
        }

        public List<Feedback> GetAllFeedbackByRoomTypId(string roomTypeId)
        {
            return feedbackRepository.GetAllByRTId(roomTypeId);
        }

        public List<Feedback> GetAllFeedbacks()
        {
            return feedbackRepository.GetAll();
        }

        public Feedback GetFeedbackById(string feedbackId)
        {
            return feedbackRepository.GetById(feedbackId);
        }

        public bool RemoveFeedback(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            var feedback = GetFeedbackById(id);

            if (feedback == null) return false;

            if (feedbackRepository.Delete(feedback) > 0) return true;
            return false;
        }

        public bool UpdateFeedback(Feedback feedback)
        {
            if (feedback == null) return false;
            if (feedbackRepository.Update(feedback) > 0) return true;
            return false;
        }

        public string GenerateCode() {
            return feedbackRepository.GetLatestSequence();
        }

        public Feedback GetFeedbackByReservationId(string reservationId)
        {
            return feedbackRepository.GetByRsId(reservationId);
        }
    }
}
