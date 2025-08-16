namespace HotelRoomReservationSystem.BLL.Interfaces
{
    public interface IRoomService
    {
        public List<Rooms> GetAllRooms();
        public List<Rooms> GetAllRoomsById(string searchBar);

        public Rooms GetRoomById(string roomId);

        public bool IsIdAvailable(string id);

        public bool IsNameAvailable(string name);
        public bool AddRoom(Rooms room);

        public void RemoveRoom(IEnumerable<string> roomIds);
        public void CommitTransaction();
        public void RollTransaction();

        public string GenerateCode();
        public Rooms GetRoomByName(string roomName);
        public List<Rooms> GetAllRoomByCategoryId(string catgeoryId);
        public string CheckNameRange(string StartRoom, string EndRoom);

        public List<string> GetExistingRoom(string StartRoom, string EndRoom);
        public List<string> GetRange(string startRoom, string endRoom);
        public bool UpdateRoomStatus(Rooms room);
        public bool UpdateRoom(Rooms room);

        public List<string> ConvertToRange(List<Rooms> roomList);

        public List<string> GetAllRoomId(string roomTypeId);

        //public string? GetRandomRoomId(List<string> roomIds, List<Reservation> allReservations, DateTime checkInDate, DateTime checkOutDate);
        public string? GetRandomRoomId(string roomIds, List<Reservation> allReservations, List<WaitingList> wt, List<string> roomDlt);

    }
}
