using HotelRoomReservationSystem.DAL.Interfaces;

namespace HotelRoomReservationSystem.DAL
{
    public class WaitingListRepository : IWaitingListRepository
    {

        private readonly HotelRoomReservationDB db;

        public WaitingListRepository(HotelRoomReservationDB db)
        {
            this.db = db;
        }

        public int Add(WaitingList wt)
        {
            db.WaitingList.Add(wt);
            return db.SaveChanges();
        }

        public List<WaitingList> GetAll()
        {
            return db.WaitingList.ToList();
        }

        public int Update(WaitingList wt)
        {
            db.WaitingList.Update(wt);
            return db.SaveChanges();
        }
    }
}
