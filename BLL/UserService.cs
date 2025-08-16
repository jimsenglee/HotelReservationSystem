using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.DAL.Interfaces;

namespace HotelRoomReservationSystem.BLL
{
    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;
        public UserService(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        public Users GetUserById(string userId)
        {
            if (userId == null) return null;
            return userRepository.GetById(userId);
        }

        public Users GetUserByEmail(string userEmail)
        {
            return userRepository.GetUserByEmail(userEmail);
        }

        public List<Users> GetAllUser()
        {
            return userRepository.GetAll();
        }
    }
}
