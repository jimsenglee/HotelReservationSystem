namespace HotelRoomReservationSystem.DAL.Interfaces
{
    public interface IRewardsRepository
    {
        public List<Rewards> GetAllRewards();

        public List<Rewards> GetRewardsById(string searchBar);

        public Rewards GetRewards(string id);

        public bool CheckId(string id);

        public bool CheckName(string name);

        public bool CheckCode(string code);

        public int UpdateRewards(Rewards reward);

        public int SaveRewards(Rewards rewards);

        public bool DeleteRewards(List<string> rewardIds);

        public string GetLastId();

        public int RewardQuantityMinus(string rewardId);
    }
}
