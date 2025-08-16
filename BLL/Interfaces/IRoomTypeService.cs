namespace HotelRoomReservationSystem.BLL.Interfaces
{
    public interface IRoomTypeService
    {
        public List<RoomType> GetAllRoomType();
        public List<string> GetRoomTypeNameList();
        public RoomType GetRoomTypeInfoByName(string name);
        public RoomType GetRoomTypeById(string Id);

        public bool CheckNameAvailability(string name);
        public bool AddRoomType(RoomType roomType);

        public string GenerateCode();
        public List<RoomType> GetAllRoomTypeById(string Id);

        public bool IsSameName(string roomTypeName);

        public bool IsSameName(string roomTypeName, string roomTypeId);
        public bool IsSameId(string roomTypeId);
        public bool UpdateRooomType(RoomType roomType);
    }
}
