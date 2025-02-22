using Microsoft.AspNetCore.Mvc;
using HotelRoomReservationSystem.DAL;
using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.DAL.Interfaces;
using HotelRoomReservationSystem.Models.ViewModels;
using HotelRoomReservationSystem.Models;

namespace HotelRoomReservationSystem.DAL.Interfaces;
public interface IMembershipRewardsRepository
{
    public void AddMembershipReward(MembershipRewards membershipReward);
    public List<MembershipRewards> GetAllByMM(string id);

    public void rewardMinus(string rewardId, string member);
}
