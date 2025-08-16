using HotelRoomReservationSystem.DAL.Interfaces;

namespace HotelRoomReservationSystem.DAL
{
    public class UserRepository : IUserRepository
    {
        private readonly HotelRoomReservationDB db;

        public UserRepository(HotelRoomReservationDB db)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public Users GetById(string id)
        {
            return db.Users.Where(u => u.Id == id).FirstOrDefault();
        }

        public Users GetUserByEmail(string email)
        {
            return db.Users.FirstOrDefault(u => u.Email == email);
        }

        public List<Users> GetAll()
        {
            return db.Users.ToList();
        }
    }
}
