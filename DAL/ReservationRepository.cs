using HotelRoomReservationSystem.DAL.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotelRoomReservationSystem.DAL
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly HotelRoomReservationDB db;

        public ReservationRepository(HotelRoomReservationDB db)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
        }
        public List<Reservation> GetAll()
        {
            return db.Reservation.ToList();
        }

        public List<Reservation> GetAll(string id)
        {
            return db.Reservation.Where(rs => rs.RoomId == id).ToList();
        }

        public void Update(Reservation reservation)
        {
            // Specific reservation to update
            
            db.Reservation.Update(reservation);
            db.SaveChanges();
        }

        public Reservation GetByRoomId(string roomId)
        {
            return db.Reservation.FirstOrDefault(r => r.RoomId == roomId);
        }

        public Reservation GetById(string id)
        {
            return db.Reservation.Where(rs => rs.Id == id).FirstOrDefault();
        }
    }
}
