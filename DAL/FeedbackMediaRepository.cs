using HotelRoomReservationSystem.DAL.Interfaces;

namespace HotelRoomReservationSystem.DAL
{
    public class FeedbackMediaRepository : IFeedbackMediaRepository
    {
        private readonly HotelRoomReservationDB db;

        public FeedbackMediaRepository(HotelRoomReservationDB db)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public int Add(FeedbackMedia fm)
        {
            db.FeedbackMedia.Add(fm);
            return db.SaveChanges();
        }

        public int Delete(FeedbackMedia fm)
        {
            db?.FeedbackMedia.Remove(fm);
            return db.SaveChanges();
        }

        public List<FeedbackMedia> GetAll()
        {
            return db.FeedbackMedia.ToList();
        }

        public List<FeedbackMedia> GetAllByFeedbackId(string id)
        {
            return db.FeedbackMedia.Where(fm => fm.FeedbackId == id).ToList();
        }

        public int Update(FeedbackMedia fm)
        {
            throw new NotImplementedException();
        }

        public int SaveMedia(FeedbackMedia fm)
        {
            // Add the new room image to the RoomTypeImagess table
            db.FeedbackMedia.Add(fm);
            return db.SaveChanges();  // Save changes to the database
        }
    }
}
