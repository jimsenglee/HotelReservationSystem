namespace HotelRoomReservationSystem.DAL.Interfaces
{
    public interface IFeedbackMediaRepository
    {
        int Add(FeedbackMedia fm);
        List<FeedbackMedia> GetAllByFeedbackId(string id);

        List<FeedbackMedia> GetAll();

        int Update(FeedbackMedia fm);
        int Delete(FeedbackMedia fm);
        public int SaveMedia(FeedbackMedia fm);
    }
}
