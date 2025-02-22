namespace HotelRoomReservationSystem.BLL.Interfaces
{
    public interface IRoomTypeImageService
    {
        public List<RoomTypeImages> GetAllRoomTypeImages();
        public List<RoomTypeImages> GetRoomTypeImagesById(string id);

        public bool IsImageFile(IFormFile file);
        public bool IsFileSizeValid(IFormFile file, long maxSizeInBytes);
        public bool IsImageCountValid(int existingImageCount, int newImageCount, int maxAllowed);
        //public string GenerateImageId(DateTime date);

        public string AddImages(IFormFile file, string roomTypeId);
        public bool IsDuplicateImage(IFormFile file, HashSet<string> fileHashes);
        public List<string> GetRoomTypeImagesNameById(string id);

        public bool Remove(string roomTypeId);

    }
}
