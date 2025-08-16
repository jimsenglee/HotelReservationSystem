using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.DAL.Interfaces;

namespace HotelRoomReservationSystem.BLL
{
    public class WaitingListService : IWaitingListService
    {
        private readonly IWaitingListRepository waitingListRepository;

        public WaitingListService(IWaitingListRepository waitingListRepository)
        {
            this.waitingListRepository = waitingListRepository;
        }

        public bool AddWaitingList(WaitingList wt)
        {
            if (waitingListRepository.Add(wt) == 1) return true;
            else return false;
        }

        public List<WaitingList> GetAllWaitingList()
        {
            return waitingListRepository.GetAll();
        }

        public bool RemoveWaitingList(WaitingList wt)
        {
            throw new NotImplementedException();
        }

        public bool UpdateWaitingList(WaitingList wt)
        {
            var result = waitingListRepository.Update(wt);
            if (result == 1) return true;
            else return false;
        }

    }
}
