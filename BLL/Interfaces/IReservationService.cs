namespace HotelRoomReservationSystem.BLL.Interfaces
{
    public interface IReservationService
    {
        public List<Reservation> GetAllReservation();
        public List<Reservation> GetAllReservationByRoomId(string id);
        public string ReAssigned(string dltRoomValue, string id, List<string> allRoom, List<Reservation> reservations);

        public Reservation getReservationById(string reservationId);
    }
}
