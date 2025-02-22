namespace HotelRoomReservationSystem.DAL.Interfaces
{
    public interface IRoomTypeRepository
    {
        public List<RoomType> GetAllDataList();
        public List<string> GetRoomTypeNames();
        public RoomType IsSameName(string roomTypeName);
        public bool CheckName(string name);
        public RoomType GetRoomTypeById(string Id);

        public int SaveRoomType(RoomType roomType);
        public string getLast();
        public List<RoomType> GetAllRoomTypeById(string Id);

        public bool Update(RoomType roomType);
    }
}
