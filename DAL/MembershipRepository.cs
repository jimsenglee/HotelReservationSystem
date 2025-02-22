using HotelRoomReservationSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelRoomReservationSystem.DAL.Interfaces;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using HotelRoomReservationSystem.DAL;
using HotelRoomReservationSystem.Models.ViewModels;
using System;
using DocumentFormat.OpenXml.Spreadsheet;

namespace HotelRoomReservationSystem.DAL;

public class MembershipRepository : IMembershipRepository
{
    private readonly HotelRoomReservationDB db;

    public MembershipRepository(HotelRoomReservationDB db)
    {
        this.db = db ?? throw new ArgumentNullException(nameof(db));
    }   

    public List<Memberships> GetAllMember()
    {
        return db.Memberships.Include(m => m.User).ToList();
    }

    public List<Memberships> GetMemberById(string searchBar)
    {
        return db.Memberships.Where(member => member.Id.Contains(searchBar)).ToList();
    }

    public Memberships GetMember(string id)
    {
        return db.Memberships.FirstOrDefault(m => m.UserId == id); // Use FirstOrDefault for more flexibility
    }


    public bool CheckId(string id) 
    {
        return !db.Memberships.Any(m => m.Id == id);
    }

    public int SaveMember(Memberships membership)
    {
        db.Memberships.Add(membership);
        return db.SaveChanges();
    }

    public int UpdateMember(MembersVM member)
    {
        var existingMember = db.Memberships.Find(member.Id);
        if (existingMember == null) return 0;

        if (existingMember.Status.Equals("Active", StringComparison.Ordinal))
        {
            existingMember.Status = "Inactive";
        }
        else
        {
            existingMember.Status = "Active";
        }

        return db.SaveChanges();
    }

    public string GetLastId()
    {
        var lastId = db.Memberships.OrderByDescending(m => m.Id).Select(m => m.Id).FirstOrDefault();
        int nextId = lastId != null ? int.Parse(lastId[1..]) + 1 : 1;
        return $"MB{nextId:D3}";
    }

    public int UpdateCheckin(string memberId, DateTime checkinDate)
    {
        var member = db.Memberships.FirstOrDefault(m => m.Id == memberId);

        if (member == null) return 0; // Member not found

        // Calculate reward based on streak
        int reward = member.Streak >= 7 ? 5 : member.Streak;

        // Update member properties
        member.LastCheckinDate = checkinDate;
        member.Points += reward;

        db.Entry(member).State = EntityState.Modified;
        db.SaveChanges();

        return reward; // Return the reward for this check-in
    }

    public bool UpdateStreak(Memberships member)
    {
        var existingMember = db.Memberships.FirstOrDefault(m => m.Id == member.Id);

        if (existingMember == null)
        {
            return false;  // No member found, return false
        }

        // Update the properties directly
        existingMember.Streak = member.Streak;
        existingMember.Points = member.Points;

        // Save the changes to the database
        db.SaveChanges();

        return true;
    }

    public void PointsMinus(string memberId, int pointsNeeded)
    {
        var member = db.Memberships.FirstOrDefault(m => m.Id == memberId);

        if (member == null)
        {
            throw new Exception("Member not found.");  // Or return an error as needed
        }

        if (member.Points >= pointsNeeded)
        {
            member.Points -= pointsNeeded;
            db.SaveChanges();
        }
    }

    public int GetPoints(string memberId)
    {
        var member = db.Memberships.FirstOrDefault(m => m.Id == memberId);

        return member != null ? member.Points : 0;
    }

    public void AddNewMember(string userId)
    {
        var lastId = db.Memberships.OrderByDescending(m => m.Id).Select(m => m.Id).FirstOrDefault();
        int nextId = lastId != null ? int.Parse(lastId[2..]) + 1 : 1;
        string memberId = $"MB{nextId:D3}";


        db.Memberships.Add(new Memberships
        {
            Id = memberId,
            StartDate = DateTime.Now,
            Loyalty = 0,
            Points = 0,
            Level = "Basic",
            UserId = userId,
            Status = "Active",
            Streak = 0
        });
        db.SaveChanges();
    }

    public void AddPointsAndLoyalty(string memberId, int point)
    {
        var existingMember = db.Memberships.FirstOrDefault(m => m.Id == memberId);

        existingMember.Points += point;
        existingMember.Loyalty += point;  

        if (existingMember.Loyalty > 1000)
        {
            existingMember.Level = "Platinum";
        }
        else if (existingMember.Loyalty > 1500)
        {
            existingMember.Level = "VIP";
        }
        db.SaveChanges();   
    }
}
