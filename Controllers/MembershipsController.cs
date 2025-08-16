using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;
using System.Linq;
using X.PagedList.Extensions;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System.Text.Json;
using HotelRoomReservationSystem.BLL;
using Azure.Core;
using Microsoft.EntityFrameworkCore;

namespace HotelRoomReservationSystem.Controllers;

public class MembershipsController : Controller
{
    private readonly IMembershipService membershipService;
    private readonly IRewardsService rewardsService;
    private readonly IMembershipRewardsService membershipRewardsService;
    private readonly ILogger<MembershipsController> _logger;

    public MembershipsController(
        IMembershipService membershipService,
        IRewardsService rewardsService,
        IMembershipRewardsService membershipRewardsService,
        ILogger<MembershipsController> logger)
    {
        this.membershipService = membershipService ?? throw new ArgumentNullException(nameof(membershipService));
        this.rewardsService = rewardsService ?? throw new ArgumentNullException(nameof(rewardsService));
        this.membershipRewardsService = membershipRewardsService ?? throw new ArgumentNullException(nameof(membershipRewardsService));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }




public IActionResult MemberManagement(string? searchBar, string? sort, string? dir, int page = 1, int pageSize = 5)
{
    // Handling empty or null searchBar and assigning default values
    ViewBag.Name = searchBar?.Trim() ?? "";
    ViewBag.PageSize = pageSize;
    ViewBag.Page = page;

    // Fetching members based on search criteria
    var members = string.IsNullOrEmpty(searchBar)
        ? membershipService.GetAllMembers() // Ensure User is loaded
        : membershipService.GetAllMembersById(searchBar);

    var memberDetails = members.Select(m => new MembersVM
    {
        Id = m.Id,
        StartDate = m.StartDate,
        Loyalty = m.Loyalty,
        Points = m.Points,
        Level = m.Level,
        UserId = m.UserId,
        Status = m.Status,
        LastCheckinDate = m.LastCheckinDate,
        Streak = m.Streak,
        UserName = m.User.Name  // Getting the username from the related User entity
    }).ToList();

    // Sorting logic
    ViewBag.Sort = sort;  // Default to "Id" if sort is null
    ViewBag.Dir = dir;    // Default to ascending order if dir is null

    Func<MembersVM, object> fn = sort switch
    {
        "MemberId" => m => m.Id,
        "StartDate" => m => m.StartDate,
        "Loyalty" => m => m.Loyalty,
        "Points" => m => m.Points,
        "MemberLevel" => m => m.Level,
        "UserId" => m => m.UserId,
        "Status" => m => m.Status,
        "LastCheckInDate" => m => m.LastCheckinDate,
        "Streak" => m => m.Streak,
        _ => m => m.Id,
    };

    var sorted = dir == "des" ? memberDetails.OrderByDescending(fn) : memberDetails.OrderBy(fn);

    // Pagination logic
    if (page < 1)
    {
        return RedirectToAction(nameof(MemberManagement), new { searchBar, sort, dir, page = 1, pageSize });
    }

    var pagedMembers = sorted.ToPagedList(page, pageSize);

    if (page > pagedMembers.PageCount && pagedMembers.PageCount > 0)
    {
        return RedirectToAction(nameof(MemberManagement), new { searchBar, sort, dir, page = pagedMembers.PageCount, pageSize });
    }

    if (Request.IsAjax())
    {
        return PartialView("_MembersList", pagedMembers);
    }

    ViewBag.PageSize = pageSize;
    ViewBag.Sort = sort;
    ViewBag.Dir = dir;
    ViewBag.Name = searchBar;
    return View("MemberManagement", pagedMembers);
}


[HttpPost]
    public IActionResult UpdateStatus([FromBody] MembersVM member)
    {
        if (member == null || string.IsNullOrEmpty(member.Id))
        {
            return Json(new { success = false, message = "Invalid member data." });
        }

        var existingMember = membershipService.GetMember(member.Id);

        if (existingMember == null)
        {
            return Json(new { success = false, message = $"Member not found. ID: {member.Id}" });
        }

        // Toggle status
        string newStatus = existingMember.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)
            ? "Inactive"
            : "Active";

        member.Status = newStatus;

        // Update member status in the database
        var result = membershipService.UpdateMember(member);

        if (result)
        {
            return Json(new { success = true, newStatus });
        }

        return Json(new { success = false, message = "Failed to update the member's status." });
    }

    public IActionResult MembershipsPage()
    {
        var userJson = HttpContext.Session.GetString("User");

        if (string.IsNullOrEmpty(userJson))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = JsonSerializer.Deserialize<Users>(userJson);

        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var member = GetCurrentMemberId(user.Id);

        if (member == null)
        {
            return RedirectToAction("Error", "Home");
        }

        var rewards = rewardsService?.GetAllRewards() ?? new List<Rewards>();
        var rewardDetails = rewards
            .Where(r => r.Status != "Inactive" && r.Quantity > 0) // Filter out inactive rewards and rewards with zero quantity
            .Select(r => new RewardsVM
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Status = r.Status,
                PointsRequired = r.PointsRequired,
                ValidFrom = r.ValidFrom,
                ValidUntil = r.ValidUntil,
                RewardCode = r.RewardCode,
                Quantity = r.Quantity,
                DiscountRate = r.DiscountRate,
            })
            .ToList();


        var memberVM = new MembersVM
        {
            Id = member.Id,
            Loyalty = member.Loyalty,
            Points = member.Points,
            Streak = CheckStreak(),
            LastCheckinDate = member.LastCheckinDate,
        };

        var viewModel = new Tuple<MembersVM, List<RewardsVM>>(memberVM, rewardDetails);

        return View(viewModel);
    }

    public int CheckStreak()
    {
       return (int)DateTime.Now.DayOfWeek;
    }

    private void UpdateStreakToMatchRealDay(Memberships member)
    {
        int todayNumber = (int)DateTime.Today.DayOfWeek;
        if (todayNumber == 0) todayNumber = 7; // Treat Sunday as day 7

        if (member.Streak < todayNumber)
        {
            member.Streak = todayNumber;
            membershipService.UpdateMemberStreak(member); ;
        }
    }

    private Memberships GetCurrentMemberId(string userId)
    {
        var member = membershipService.GetMember(userId);

        if (member == null)
        {
            return null;
        }

        // Update streak based on the current day
        UpdateStreakToMatchRealDay(member);

        // Ensure the updated streak gets saved after the update
        membershipService.UpdateMemberStreak(member);

        return member;
    }

    [HttpPost]
    public JsonResult CheckIn()
    {
        // Retrieve the User JSON from session
        var userJson = HttpContext.Session.GetString("User");

        if (string.IsNullOrEmpty(userJson))
        {
            return Json(new { success = false, message = "User not logged in." });
        }

        // Deserialize the user data
        var user = JsonSerializer.Deserialize<Users>(userJson);

        if (user == null)
        {
            return Json(new { success = false, message = "Invalid User." });
        }

        var member = GetCurrentMemberId(user.Id);

        if (member == null)
        {
            return Json(new { success = false, message = "Member not found." });
        }

        DateTime today = DateTime.Today;

        // Prevent multiple check-ins on the same day
        if (member.LastCheckinDate.HasValue && member.LastCheckinDate.Value.Date == today)
        {
            return Json(new { success = false, message = "You have already checked in today!" });
        }

        // Update the streak and calculate rewards
        int reward = membershipService.UpdateCheckin(member.Id, today);

        if (reward > 0)
        {
            return Json(new
            {
                success = true,
                message = $"Check-in successful! You earned {reward} coins.",
                streakCount = CheckStreak(),
                totalCoins = member.Points,
                today = today.ToString("yyyy-MM-dd"),
                dayOfWeek = today.DayOfWeek.ToString()
            });
        }

        return Json(new { success = false, message = "Failed to update check-in. Please try again." });
    }

    [HttpPost]
    public IActionResult ClaimReward(string rewardId, string memberId, int pointsRequired)
    {
        if (string.IsNullOrEmpty(rewardId))
        {
            return Json(new { success = false, message = "Invalid Reward ID." });
        }

        if (string.IsNullOrEmpty(memberId))
        {
            return Json(new { success = false, message = "Invalid User ID." });
        }

        try
        {
            // Retrieve member's points from the database (use the appropriate service to fetch this info)
            var memberPoints = membershipService.GetPoints(memberId);  // Ensure this method gets the current points from the database

            // Ensure the member has enough points to claim the reward
            if (memberPoints < pointsRequired || memberPoints <= 0)
            {
                return Json(new { success = false, message = "You do not have enough points to claim this reward." });
            }

            // Claim the reward and update the database with member information
            membershipRewardsService.ClaimReward(rewardId, memberId);

            // Decrease the quantity of the reward
            int quantity = rewardsService.RewardQuantityMinus(rewardId);

            if (quantity == 0)
            {
                return Json(new { success = false, message = "Reward is Empty"});
            }

            // Deduct points
            membershipService.PointsMinus(memberId, pointsRequired);

            // Get the updated member points after deduction
            var updatedPoints = membershipService.GetPoints(memberId);

            return Json(new { success = true, message = "Reward claimed successfully!", updatedPoints = updatedPoints });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error claiming reward");
            return Json(new { success = false, message = "An error occurred while claiming the reward." });
        }
    }


}