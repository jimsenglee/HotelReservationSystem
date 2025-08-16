namespace HotelRoomReservationSystem.DAL.Interfaces
{
    public interface IFeedbackRepository
    {
        int Add(Feedback feedback);

        int Update(Feedback feedback);

        int Delete(Feedback feedback);

        List<Feedback> GetAll();

        Feedback GetById(string id);

        List<Feedback> GetAllByRTId(string id);
        public string GetLatestSequence();
        public Feedback GetByRsId(string reservationId);
    }
}
