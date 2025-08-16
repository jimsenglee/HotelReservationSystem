namespace HotelRoomReservationSystem.BLL.Interfaces
{
    public interface IWaitingListService
    {
        List<WaitingList> GetAllWaitingList();
        //public List<WaitingList> GetAllWaitingListById(string id);

        public bool UpdateWaitingList(WaitingList wt);

        bool AddWaitingList(WaitingList wt);

        bool RemoveWaitingList(WaitingList wt);
    }
}
