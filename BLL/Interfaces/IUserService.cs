namespace HotelRoomReservationSystem.BLL.Interfaces
{
    public interface IUserService
    {
        Users GetUserById(string userId);
        public Users GetUserByEmail(string userEmail);
        public List<Users> GetAllUser();

    }
}
