using HotelRoomReservationSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelRoomReservationSystem.DAL.Interfaces;


namespace HotelRoomReservationSystem.DAL;

public class RewardsRepository : IRewardsRepository
{
    private readonly HotelRoomReservationDB db;

    public RewardsRepository(HotelRoomReservationDB db)
    {
        this.db = db ?? throw new ArgumentNullException(nameof(db));    
    }

    public List<Rewards> GetAllRewards()
    {
        return db.Rewards.ToList();
    }

    public List<Rewards> GetRewardsById(string searchBar)
    {
        return db.Rewards.Where(reward => reward.Name.Contains(searchBar) || reward.Id.ToString().Contains(searchBar)).ToList();
    }

    public Rewards GetRewards(string id)
    {
        return db.Rewards.Find(id);
    }

    public bool CheckId(string id)
    {
        return !db.Rewards.Any(r => r.Id == id);
    }

    public bool CheckName(string name)
    {
        return !db.Rewards.Any(r => r.Name.Equals(name));
    }
    public bool CheckCode(string code)
    {
        return !db.Rewards.Any( r => r.RewardCode.Equals(code));
    }

    public int UpdateRewards(Rewards reward)
    {
        // Get the existing reward
        var existingReward = db.Rewards.Find(reward.Id);
        if (existingReward == null) return 0;  // Reward not found

        // Update the properties
        existingReward.Name = reward.Name;
        existingReward.RewardCode = reward.RewardCode;

        // Save changes to the database
        return db.SaveChanges();
    }


    public int SaveRewards(Rewards reward)
    {
        db.Rewards.Add(reward);
        return db.SaveChanges();
    }


    public bool DeleteRewards(List<string> rewardIds)
    {
        // Ensure rewardIds is not null or empty to prevent accidental updates to all records
        if (rewardIds == null || !rewardIds.Any())
        {
            return false;
        }

        // Retrieve the rewards to update
        var rewardsToUpdate = db.Rewards
                                .Where(r => rewardIds.Contains(r.Id))
                                .ToList();

        foreach (var reward in rewardsToUpdate)
        {
            // Toggle the status between "Inactive" and "Active"
            if (reward.Status == "Inactive")
            {
                reward.Status = "Active"; // Change to Active if it's currently Inactive
            }
            else
            {
                reward.Status = "Inactive"; // Change to Inactive if it's currently Active
            }
        }

        // Save changes to the database
        db.SaveChanges();

        return true;
    }

    public string GetLastId()
    {
        var lastId = db.Rewards.OrderByDescending(r => r.Id).Select(r => r.Id).FirstOrDefault();
        int nextId = lastId != null ? int.Parse(lastId[1..]) + 1 : 1;
        return $"R{nextId:D3}";
    }

    public int RewardQuantityMinus(string rewardId)
    {
        var reward = db.Rewards
                       .FirstOrDefault(r => r.Id == rewardId);

        if (reward != null)
        {
            if (reward.Quantity > 0)
            {
                var existQauntity = reward.Quantity;
                reward.Quantity -= 1;
                db.SaveChanges();
                return existQauntity;
            }
            else
            {
                return reward.Quantity;
            }
        }
        else
        {
            throw new InvalidOperationException("Reward not found.");
        }
    }



}
