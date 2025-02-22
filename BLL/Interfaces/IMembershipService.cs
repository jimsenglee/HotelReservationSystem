using Microsoft.AspNetCore.Mvc;
using HotelRoomReservationSystem.DAL;
using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.DAL.Interfaces;
using HotelRoomReservationSystem.Models.ViewModels;

namespace HotelRoomReservationSystem.BLL.Interfaces;

public interface IMembershipService
{
    public List<Memberships> GetAllMembers();

    public List<Memberships> GetAllMembersById(string searchBar);

    public Memberships GetMember(string id);

    public bool CheckId(string Id);

    public bool AddMember(Memberships member);

    public bool UpdateMember(MembersVM member);

    public string GetLastId();

    public int UpdateCheckin(string memberId, DateTime checkinDate);

    public bool UpdateMemberStreak(Memberships member);

    public void PointsMinus(string memberId, int pointsNeeded);

    public int GetPoints(string memberId);

    public void AddNewMember(string userId);

    public void AddPointsAndLoyalty(string memberId, int point);
}
