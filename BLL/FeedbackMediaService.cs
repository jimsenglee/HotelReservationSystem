using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.DAL;
using HotelRoomReservationSystem.DAL.Interfaces;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotelRoomReservationSystem.BLL
{
    public class FeedbackMediaService : IFeedbackMediaService
    {
        private readonly IFeedbackMediaRepository feedbackMediaRepository;

        public FeedbackMediaService(IFeedbackMediaRepository feedbackMediaRepository)
        {
            this.feedbackMediaRepository = feedbackMediaRepository;
        }

        public bool AddFeedbackMedia(FeedbackMedia fm)
        {
            if (fm == null) throw new ArgumentNullException(nameof(fm));
            if (feedbackMediaRepository.Add(fm) > 0) return true;
            return false;
        }

        public List<FeedbackMedia> GetAllFeebackMedia()
        {
            return feedbackMediaRepository.GetAll();
        }

        public List<FeedbackMedia> GetAllFeedbackMediaByFeedbackId(string feedbackId)
        {
            return feedbackMediaRepository.GetAllByFeedbackId(feedbackId);
        }

        public bool RemoveFeedbackMedia(FeedbackMedia fm)
        {
            if (fm == null) throw new ArgumentNullException(nameof(fm));
            if (feedbackMediaRepository.Delete(fm) > 0) return true;
            return false;
        }

        public bool UpdateFeedbackMedia(FeedbackMedia fm)
        {
            if (fm == null) throw new ArgumentNullException(nameof(fm));
            if (feedbackMediaRepository.Update(fm) > 0) return true;
            return false;
        }

        public string AddImages(IFormFile file, string feedbackId)
        {
            // Define a directory path to store uploaded files
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

            // Ensure the directory exists
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Create a unique file name to avoid collisions
            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";

            // Combine the upload path and file name
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            try
            {
                // Save the file to the server
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                // Create a new RoomTypeImagess object with the file name and metadata
                var fm = new FeedbackMedia
                {
                    Name = uniqueFileName,      // File name saved on the server
                    FileType = file.ContentType,
                    FeedbackId = feedbackId, // The room this image belongs to
                };

                // Save the image record in the database
                if (feedbackMediaRepository.SaveMedia(fm) > 0)
                {
                    return uniqueFileName;
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Console.WriteLine($"Error saving image: {ex.Message}");
            }

            return "";
        }

    }
}
