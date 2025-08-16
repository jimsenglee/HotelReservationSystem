using HotelRoomReservationSystem.DAL.Interfaces;
using HotelRoomReservationSystem.Models;

namespace HotelRoomReservationSystem.DAL
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly HotelRoomReservationDB db;

        public FeedbackRepository(HotelRoomReservationDB db)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public int Add(Feedback feedback)
        {
            db.Feedback.Add(feedback);
            return db.SaveChanges();
        }

        public List<Feedback> GetAll()
        {
            return db.Feedback.ToList();
        }

        public Feedback GetById(string id)
        {
            return db.Feedback.Where(f => f.Id == id).FirstOrDefault();
        }

        public int Update(Feedback feedback)
        {
            db.Feedback.Update(feedback);
            return db.SaveChanges();
        }

        public List<Feedback> GetAllByRTId(string id)
        {
            return db.Feedback.Where(f => f.Id.Contains(id) || f.Description.Contains(id)).ToList();
        }

        public int Delete(Feedback feedback)
        {
            db.Feedback.Remove(feedback);
            return db.SaveChanges();
        }

        public string GetLatestSequence()
        {
            string prefix = "FB"; // Prefix for the ID
            string datePrefix = DateTime.Now.ToString("yyMMdd"); // Current date in yyMMdd format
            string fullPrefix = prefix + datePrefix;

            // Find the latest ID with the same prefix
            var latestId = db.Feedback
                             .Where(r => r.Id.StartsWith(fullPrefix))
                             .OrderByDescending(r => r.Id)
                             .Select(r => r.Id)
                             .FirstOrDefault();

            int sequence = 0;

            if (latestId != null)
            {
                // Extract the numeric part of the ID (the sequence)
                sequence = int.Parse(latestId.Substring(fullPrefix.Length));
            }

            // Increment the sequence and format it to 3 digits
            sequence++;

            // Construct the new ID
            return $"{fullPrefix}{sequence:D3}";
        }

        public Feedback GetByRsId(string reservationId)
        {
            return db.Feedback.Where(fb => fb.ReservationId == reservationId).FirstOrDefault();
        }

    }
}
