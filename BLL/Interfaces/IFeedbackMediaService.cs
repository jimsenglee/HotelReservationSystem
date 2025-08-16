using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace HotelRoomReservationSystem.BLL.Interfaces
{
    public interface IFeedbackMediaService
    {

        bool AddFeedbackMedia(FeedbackMedia fm);
        bool RemoveFeedbackMedia(FeedbackMedia fm);

        bool UpdateFeedbackMedia(FeedbackMedia fm);

        List<FeedbackMedia> GetAllFeebackMedia();

        List<FeedbackMedia> GetAllFeedbackMediaByFeedbackId(string feedbackId);

        public string AddImages(IFormFile file, string feedbackId);
    }
}
