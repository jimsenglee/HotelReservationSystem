namespace HotelRoomReservationSystem.DAL.Interfaces
{
    public interface IWaitingListRepository
    {
        List<WaitingList> GetAll();

        int Update(WaitingList wt);

        int Add(WaitingList wt);
    }
}
