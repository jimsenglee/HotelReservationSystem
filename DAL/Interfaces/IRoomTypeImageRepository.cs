namespace HotelRoomReservationSystem.DAL.Interfaces
{
    public interface IRoomTypeImageRepository
    {
        public List<RoomTypeImages> GetAllDataList();
        public List<RoomTypeImages> GetRoomTypeImagesById(string id);

        //public int GetLatestSequence();
        public int SaveRoomTypeImages(RoomTypeImages roomTypeImage);
        public List<string> GetNameById(string id);

        public int Remove(string id);

    }
}
