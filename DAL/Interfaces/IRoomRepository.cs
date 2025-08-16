namespace HotelRoomReservationSystem.DAL.Interfaces
{
    public interface IRoomRepository
    {
        public List<Rooms> GetAllRooms();
        public List<Rooms> GetRoomsById(string searchBar);

        public Rooms GetRoom(string roomId);
        public bool CheckId(string id);
        public bool CheckName(string name);
        public int SaveRoom(Rooms room);

        public void Delete(string roomId);

        public string getLast();

        public void BeginTransaction();
        public void CommitTransaction();
        public void RollbackTransaction();
        public void SaveChanges();
        public List<Rooms> GetDataByCtgId(string catgeoryId);
        public Rooms GetDataByName(string roomName);
        public bool Update(Rooms room);
    }
}
