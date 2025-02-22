using HotelRoomReservationSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HotelRoomReservationSystem.BLL.Interfaces
{
    public interface IRewardsService
    {
        public List<Rewards> GetAllRewards();

        public List<Rewards> GetAllRewardsById(string searchBar);
 
        public Rewards GetRewards(string id);

        public bool IsIdAvailable(string id);

        public bool IsNameAvailable(string name);

        public bool IsCodeAvailable(string code);

        public bool AddRewards(Rewards rewards);

        public bool UpdateRewards(UpdateRewardsVM updatedReward);

        public bool DeleteRewards(List<string> rewardIds);

        public string GetLastId();

        public int RewardQuantityMinus(string rewardId);
    }
}
