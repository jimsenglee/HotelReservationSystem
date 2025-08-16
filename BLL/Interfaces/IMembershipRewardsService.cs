using Microsoft.AspNetCore.Mvc;
using HotelRoomReservationSystem.DAL;
using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.DAL.Interfaces;
using HotelRoomReservationSystem.Models.ViewModels;

namespace HotelRoomReservationSystem.BLL.Interfaces;

public interface IMembershipRewardsService
{
    public void ClaimReward(string rewardId, string userId);
    public List<MembershipRewards> GetAllByMM(string id);

    public void rewardMinus(string rewardId, string member);
}
