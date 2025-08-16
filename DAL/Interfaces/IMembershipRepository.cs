using Microsoft.AspNetCore.Mvc;
using HotelRoomReservationSystem.DAL;
using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.DAL.Interfaces;
using HotelRoomReservationSystem.Models.ViewModels;

namespace HotelRoomReservationSystem.DAL.Interfaces;

public interface IMembershipRepository
{
    public List<Memberships> GetAllMember();

    public List<Memberships> GetMemberById(string searchBar);

    public Memberships GetMember(string id);

    public bool CheckId(string id);

    public int SaveMember(Memberships membership);

    public int UpdateMember(MembersVM member);

    public string GetLastId();

    public int UpdateCheckin(string memberId, DateTime checkinDate);

    public bool UpdateStreak(Memberships member);

    public void PointsMinus(string memberId, int pointsNeeded);

    public int GetPoints(string memberId);

    public void AddNewMember(string userId);

    public void AddPointsAndLoyalty(string memberId, int point);
}
