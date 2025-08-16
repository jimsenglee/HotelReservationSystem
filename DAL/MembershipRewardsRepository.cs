using HotelRoomReservationSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelRoomReservationSystem.DAL.Interfaces;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using HotelRoomReservationSystem.DAL;
using HotelRoomReservationSystem.Models.ViewModels;
using System;

namespace HotelRoomReservationSystem.DAL;
public class MembershipRewardsRepository : IMembershipRewardsRepository
{
    private readonly HotelRoomReservationDB db;

    public MembershipRewardsRepository(HotelRoomReservationDB db)
    {
        this.db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public void AddMembershipReward(MembershipRewards membershipReward)
    {
        // Check if the membership reward already exists in the database
        var existingReward = db.MembershipRewards
            .FirstOrDefault(r => r.RewardId == membershipReward.RewardId && r.MembershipId == membershipReward.MembershipId);

        if (existingReward != null)
        {
            // If the reward already exists, increment the quantity
            existingReward.Quantity += 1;
        }
        else
        {
            // If the reward doesn't exist, add the new membership reward
            db.MembershipRewards.Add(membershipReward);
        }



        // Save the changes to the database
        db.SaveChanges();
    }

    public List<MembershipRewards> GetAllByMM(string id)
    {
        return db.MembershipRewards
         .Where(mm => mm.MembershipId == id && mm.Quantity > 0)
         .ToList();

    }

    public void rewardMinus(string rewardId, string member)
    {
        var existingReward = db.MembershipRewards
        .FirstOrDefault(r => r.RewardId == rewardId && r.MembershipId == member);

        existingReward.Quantity -= 1;
    }
}
