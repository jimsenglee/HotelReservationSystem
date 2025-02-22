using HotelRoomReservationSystem.DAL;
using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.DAL.Interfaces;
using System.Reflection;

namespace HotelRoomReservationSystem.BLL
{
    public class RoomTypeImageService: IRoomTypeImageService
    {
        private readonly IRoomTypeImageRepository roomTypeImageRepository;

        public RoomTypeImageService(IRoomTypeImageRepository roomTypeImageRepository)
        {
            this.roomTypeImageRepository = roomTypeImageRepository;
        }

        public bool IsImageFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            return allowedExtensions.Contains(extension);
        }

        public bool IsFileSizeValid(IFormFile file, long maxSizeInBytes)
        {
            return file.Length <= maxSizeInBytes;
        }

        public bool IsImageCountValid(int existingImageCount, int newImageCount, int maxAllowed)
        {
            return (existingImageCount + newImageCount) <= maxAllowed;
        }

        //public string GenerateImageId(DateTime date)
        //{
        //    int latestSequence = roomTypeImageRepository.GetLatestSequence();
        //    return date.ToString("yyMMdd") + (latestSequence + 1).ToString("D3");
        //}


        //public int GetSize()
        //{
        //    return roomTypeImageRepository.GetLatestSequence();
        //}

        public string AddImages(IFormFile file, string roomTypeId)
        {
            //// Define a directory path to store uploaded files
            //var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

            //// Ensure the directory exists
            //if (!Directory.Exists(uploadPath))
            //{
            //    Directory.CreateDirectory(uploadPath);
            //}
                
            // Create a unique file name to avoid collisions
            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";

            // Combine the upload path and file name
            //var filePath = Path.Combine(uploadPath, uniqueFileName);

            try
            {
                // Save the file to the server
                //using (var stream = new FileStream(filePath, FileMode.Create))
                //{
                //    file.CopyTo(stream);
                //}

                // Create a new RoomTypeImagess object with the file name and metadata
                var roomTypeImages = new RoomTypeImages
                {
                    //Id = imageId,               // Unique ID for the image
                    Name = uniqueFileName,      // File name saved on the server
                    FileType = file.ContentType,
                    RoomTypeId = roomTypeId             // The room this image belongs to
                };

                // Save the image record in the database
                if (roomTypeImageRepository.SaveRoomTypeImages(roomTypeImages) > 0)
                {
                    Console.WriteLine("---------------------------------------------------------------------------------------");
                    Console.WriteLine("Success");
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


        // Function to check if an image is a duplicate
        public bool IsDuplicateImage(IFormFile file, HashSet<string> fileHashes)
        {
            using (var stream = file.OpenReadStream())
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    // Compute hash of the file
                    var hashBytes = sha256.ComputeHash(stream);
                    var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                    // Check if this hash already exists in the set
                    if (fileHashes.Contains(hashString))
                    {
                        return true; // Duplicate image found
                    }

                    // Add hash to the set
                    fileHashes.Add(hashString);
                }
            }

            return false; // No duplicate
        }

        public List<RoomTypeImages> GetAllRoomTypeImages()
        {
            return roomTypeImageRepository.GetAllDataList();
        }

        public List<RoomTypeImages> GetRoomTypeImagesById(string id)
        {
            return roomTypeImageRepository.GetRoomTypeImagesById(id);
        }

        public List<string> GetRoomTypeImagesNameById(string id)
        {
            return roomTypeImageRepository.GetNameById(id);
        }

        public bool Remove(string roomTypeId)
        {
            if (roomTypeId == null) return false;

            if(roomTypeImageRepository.Remove(roomTypeId) > 0 ) return true;

            return false;
        }

    }
}
