namespace HotelRoomReservationSystem.DAL.Interfaces
{
    public interface IReservationRepository
    {

        List<Reservation> GetAll();
        List<Reservation> GetAll(string id);
        //Reservation GetData(string id);

        public void Update(Reservation reservation);

        public Reservation GetByRoomId(string roomId);
        public Reservation GetById(string id);

    }
}
