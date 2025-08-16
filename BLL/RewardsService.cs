using HotelRoomReservationSystem.DAL;
using Microsoft.AspNetCore.Mvc;
using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.DAL.Interfaces;
using HotelRoomReservationSystem.Models.ViewModels;
using HotelRoomReservationSystem.Models;

namespace HotelRoomReservationSystem.BLL;

public class RewardsService : IRewardsService
{
    private readonly IRewardsRepository rewardsRepository;

    public RewardsService(IRewardsRepository rewardsRepository)
    {
        this.rewardsRepository = rewardsRepository;
    }

    public List<Rewards> GetAllRewards()
    {
        return rewardsRepository.GetAllRewards();
    }

    public List<Rewards> GetAllRewardsById(string searchBar)
    {
        return rewardsRepository.GetRewardsById(searchBar);
    }

    public Rewards GetRewards(string id)
    {
        return rewardsRepository.GetRewards(id);
    }

    public bool IsIdAvailable(string Id)
    {
        return rewardsRepository.CheckId(Id);   
    }

    public bool IsNameAvailable(string Name) 
    { 
        return rewardsRepository.CheckName(Name);   
    }    

    public bool IsCodeAvailable(string Code)
    {
        return rewardsRepository.CheckCode(Code);   
    }

    public bool AddRewards(Rewards rewards)
    {
        int i = rewardsRepository.SaveRewards(rewards);
        if(i == 0)
        {
            return true;
        }
        return false;
    }

    public bool UpdateRewards(UpdateRewardsVM updatedReward)
    {
        var existingReward = rewardsRepository.GetRewards(updatedReward.Id);
        if (existingReward == null) return false;

        existingReward.RewardCode = updatedReward.RewardCode;
        existingReward.Description = updatedReward.Description;
        existingReward.ValidFrom = updatedReward.ValidFrom;
        existingReward.ValidFrom = updatedReward.ValidFrom;
        existingReward.PointsRequired = updatedReward.PointsRequired;
        existingReward.Quantity = updatedReward.Quantity;
        existingReward.Status = updatedReward.Status;
        existingReward.DiscountRate = updatedReward.DiscountRate;

        return rewardsRepository.UpdateRewards(existingReward) > 0;
    }

    public bool DeleteRewards(List<string> rewardIds)
    {
        return rewardsRepository.DeleteRewards(rewardIds);
    }

    public string GetLastId()
    {
        return rewardsRepository.GetLastId();
    }

    public int RewardQuantityMinus(string rewardId)
    {
        return rewardsRepository.RewardQuantityMinus(rewardId);
    }

}
