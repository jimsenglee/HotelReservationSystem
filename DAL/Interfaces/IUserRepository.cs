namespace HotelRoomReservationSystem.DAL.Interfaces
{
    public interface IUserRepository
    {
        Users GetById(string id);
        public Users GetUserByEmail(string email);
        public List<Users> GetAll();

    }
}
