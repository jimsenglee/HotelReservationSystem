using HotelRoomReservationSystem.DAL.Interfaces;
using HotelRoomReservationSystem.Models;
using HotelRoomReservationSystem.Models.ViewModels;
using HotelRoomReservationSystem.BLL.Interfaces;
using System;

namespace HotelRoomReservationSystem.BLL;

public class MembershipService : IMembershipService
{
    private readonly IMembershipRepository _membershipRepository;

    public MembershipService(IMembershipRepository membershipRepository)
    {
        _membershipRepository = membershipRepository ?? throw new ArgumentNullException(nameof(membershipRepository));
    }

    public List<Memberships> GetAllMembers()
    {
        return _membershipRepository.GetAllMember();
    }

    public List<Memberships> GetAllMembersById(string searchBar)
    {
        return _membershipRepository.GetMemberById(searchBar);
    }

    public Memberships GetMember(string id)
    {
        return _membershipRepository.GetMember(id);
    }

    public bool CheckId(string id)
    {
        return _membershipRepository.CheckId(id);
    }

    public bool AddMember(Memberships member)
    {
        if (member == null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        return _membershipRepository.SaveMember(member) > 0;
    }

    public bool UpdateMember(MembersVM member)
    {
        if (member == null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        return _membershipRepository.UpdateMember(member) > 0;
    }

    public string GetLastId()
    {
        return _membershipRepository.GetLastId();
    }

    public int UpdateCheckin(string memberId, DateTime checkinDate)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            throw new ArgumentException("Member ID cannot be null or empty.", nameof(memberId));
        }

        return _membershipRepository.UpdateCheckin(memberId, checkinDate);
    }

    public bool UpdateMemberStreak(Memberships member)
    {
        if (member == null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        return _membershipRepository.UpdateStreak(member);
    }

    public void PointsMinus(string memberId, int pointsNeeded)
    {
        _membershipRepository.PointsMinus(memberId, pointsNeeded);
    }

    public int GetPoints(string memberId)
    {
        return _membershipRepository.GetPoints(memberId);
    }

    public void AddNewMember(string userId)
    {
        _membershipRepository.AddNewMember(userId);
    }

    public void AddPointsAndLoyalty(string memberId, int point)
    {
        _membershipRepository.AddPointsAndLoyalty(memberId, point);
    }
}
