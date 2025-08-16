using HotelRoomReservationSystem.DAL.Interfaces;
using HotelRoomReservationSystem.Models;
using HotelRoomReservationSystem.Models.ViewModels;
using HotelRoomReservationSystem.BLL.Interfaces;
using System;

namespace HotelRoomReservationSystem.BLL;

public class MembershipRewardsService : IMembershipRewardsService
{
    private readonly IMembershipRewardsRepository membershipRewardsRepository;

    public MembershipRewardsService(IMembershipRewardsRepository membershipRewardsRepository)
    {
        this.membershipRewardsRepository = membershipRewardsRepository ?? throw new ArgumentNullException(nameof(membershipRewardsRepository));
    }

    public void ClaimReward(string rewardId, string userId)
    {
        var membershipReward = new MembershipRewards
        {
            MembershipId = userId,
            RewardId = rewardId,
            Quantity = 1
        };

        membershipRewardsRepository.AddMembershipReward(membershipReward);
    }

    public List<MembershipRewards> GetAllByMM(string memberId)
    {
        return membershipRewardsRepository.GetAllByMM(memberId);
    }

    public void rewardMinus(string rewardId, string member)
    {
        membershipRewardsRepository.rewardMinus(rewardId, member);  
    }
}
