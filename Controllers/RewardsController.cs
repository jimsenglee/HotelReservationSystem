using HotelRoomReservationSystem.BLL;
using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.Models;
using HotelRoomReservationSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;
using System.Linq;
using X.PagedList.Extensions;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Azure;
using Azure.Core;

namespace HotelRoomReservationSystem.Controllers;

public class RewardsController : Controller
{
    private readonly IRewardsService rewardsService;
    private readonly IMembershipRewardsService membershipRewardsService;

    public RewardsController(IRewardsService rewardsService, IMembershipRewardsService membershipRewardsService)
    {
        this.rewardsService = rewardsService ?? throw new ArgumentNullException(nameof(rewardsService));
        this.membershipRewardsService = membershipRewardsService ?? throw new ArgumentNullException(nameof(membershipRewardsService));
    }

    // GET: Rewards Management (with search, sorting, and pagination)
    public IActionResult RewardsManagement(string? searchBar, string? sort, string? dir, int page = 1, int pageSize = 5)
    {
        ViewBag.Name = searchBar = searchBar?.Trim() ?? "";
        ViewBag.PageSize = pageSize;
        ViewBag.Page = page;

        var rewards = string.IsNullOrEmpty(searchBar)
            ? rewardsService.GetAllRewards()
            : rewardsService.GetAllRewardsById(searchBar);

        var rewardDetails = rewards.Select(r => new RewardsVM
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
        }).ToList();

        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        // Sorting logic
        Func<RewardsVM, object> fn = sort switch
        {
            "No" => r => r.Id,                          // Sort by ID or "No"
            "Name" => r => r.Name,                      // Sort by Name alphabetically
            "Points" => r => r.PointsRequired,           // Sort by PointsRequired
            "Start Date" => r => r.ValidFrom,           // Sort by ValidFrom (Start Date)
            "End Date" => r => r.ValidUntil,            // Sort by ValidUntil (End Date)
            "Status" => r => r.Status,                  // Sort by Status
            "Reward Code" => r => r.RewardCode,         // Sort by RewardCode
            "Quantity" => r => r.Quantity,              // Sort by Quantity
            "Discount Rate" => r => r.DiscountRate,     // Sort by DiscountRate
            _ => r => r.Id                              // Default sorting by ID
        };

        var sorted = dir == "des" ? rewardDetails.OrderByDescending(fn) : rewardDetails.OrderBy(fn);

        // Pagination logic
        if (page < 1)
        {
            return RedirectToAction(nameof(RewardsManagement), new { searchBar, sort, dir, page = 1, pageSize });
        }

        var pagedRewards = sorted.ToPagedList(page, pageSize);

        if (page > pagedRewards.PageCount && pagedRewards.PageCount > 0)
        {
            return RedirectToAction(nameof(RewardsManagement), new { searchBar, sort, dir, page = pagedRewards.PageCount, pageSize });
        }

        if (Request.IsAjax())
        {
            return PartialView("_RewardsList", pagedRewards);
        }

        ViewBag.PageSize = pageSize;
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;
        ViewBag.Name = searchBar;
        return View("RewardsManagement", pagedRewards);
    }


    // GET: Create Info Form
    public IActionResult CreateInfoForm()
    {
        var lastId = new RewardsVM
        {
            Id = rewardsService.GetLastId(),
        };
        return View("CreateInfoForm", lastId);
    }

    // Check if Reward ID is available
    public bool CheckIdAvailability(string id)
    {
        return rewardsService.IsIdAvailable(id);
    }

    // Check if Reward Name is available
    public bool CheckNameAvailability(string name)
    {
        return rewardsService.IsNameAvailable(name);
    }

    // Check if Reward Code is available
    public bool CheckRewardCodeAvailability(string code)
    {
        return rewardsService.IsCodeAvailable(code);
    }

    // GET: Get Reward Details for updating
    [HttpGet]
    [Route("Rewards/getRewardDetails")]
    public IActionResult GetRewardDetails(string rewardId)
    {
        try
        {
            var reward = rewardsService.GetRewards(rewardId);

            if (reward == null)
            {
                return NotFound(new { Message = "Reward not found." });
            }

            return Json(reward);
        }
        catch (Exception ex)
        {
            // Log the exception (or use a logger framework)
            Console.WriteLine(ex);

            return StatusCode(500, new { Message = "An error occurred while fetching reward details." });
        }
    }

    // POST: Submit Create Form
    [HttpPost]
    public IActionResult SubmitForm(RewardsVM rewardsVM)
    {
        if (ModelState.IsValid)
        {
            if (rewardsVM.ValidFrom.Year != DateTime.Now.Year)
            {
                ModelState.AddModelError("ValidFrom", "The Valid From date must be in the current year.");
            }
            if (rewardsVM.ValidUntil.HasValue && rewardsVM.ValidUntil < rewardsVM.ValidFrom)
            {
                ModelState.AddModelError("ValidUntil", "The Valid Until date cannot be earlier than the Valid From date.");
            }
            if (!CheckIdAvailability(rewardsVM.Id))
            {
                ModelState.AddModelError("Id", "Duplicated Id.");
            }
            if (!CheckNameAvailability(rewardsVM.Name))
            {
                ModelState.AddModelError("Name", "Duplicated Name.");
            }
            if (!CheckRewardCodeAvailability(rewardsVM.RewardCode))
            {
                ModelState.AddModelError("RewardCode", "Duplicated Code.");
            }

            if (ModelState.IsValid)
            {
                // Save the new reward
                rewardsService.AddRewards(new Rewards
                {
                    Id = rewardsVM.Id,
                    Name = rewardsVM.Name,
                    Description = rewardsVM.Description,
                    Status = rewardsVM.Status,
                    PointsRequired = rewardsVM.PointsRequired,
                    ValidFrom = rewardsVM.ValidFrom,
                    ValidUntil = rewardsVM.ValidUntil,
                    RewardCode = rewardsVM.RewardCode,
                    Quantity = rewardsVM.Quantity,
                    DiscountRate = rewardsVM.DiscountRate
                });

                return RedirectToAction("RewardsManagement");
            }
        }

        // If validation fails, return to the form with error messages
        return View("CreateInfoForm", rewardsVM);
    }

    //Update Rewards 
    [HttpPost]
    public IActionResult UpdatedReward(UpdateRewardsVM updateRewardsVM)
    {
        if (ModelState.IsValid)
        {
            // Update reward logic here
            bool isUpdated = rewardsService.UpdateRewards(updateRewardsVM);

            if (isUpdated)
            {
                // Redirect to Rewards Management page after successful update
                return RedirectToAction("RewardsManagement");
            }
            else
            {
                // Handle the case where the update failed
                ModelState.AddModelError(string.Empty, "An error occurred while updating the reward.");
            }
        }

        // If validation fails, return the form with the errors
        return View("UpdateForm", updateRewardsVM);
    }


    // GET: Update Form for a specific Reward
    [HttpGet]
    [Route("Rewards/Details")]
    public IActionResult UpdateForm(string rewardId)
    {
        try
        {
            var reward = rewardsService.GetRewards(rewardId);

            if (reward == null)
            {
                // Redirect to an error page or show a "not found" message
                return RedirectToAction("Error", new { message = "Reward not found." });
            }

            var rewardDetails = new UpdateRewardsVM
            {
                Id = reward.Id,
                Name = reward.Name,
                Description = reward.Description,
                Status = reward.Status,
                PointsRequired = reward.PointsRequired,
                ValidFrom = reward.ValidFrom,
                ValidUntil = reward.ValidUntil,
                RewardCode = reward.RewardCode,
                Quantity = reward.Quantity,
                DiscountRate = reward.DiscountRate, 
            };

            return View("UpdateForm", rewardDetails);
        }
        catch (Exception ex)
        {
            // Log the exception (or use a logger framework)
            Console.WriteLine(ex);

            return RedirectToAction("Error", new { message = "An error occurred while loading the reward details." });
        }
    }

    //Delete Rewards
    [HttpPost]
    public IActionResult DeleteSelectedItems([FromBody] List<string> selectedRewards)
    {
        if (selectedRewards == null || !selectedRewards.Any())
        {
            return Json(new { success = false, message = "No rewards selected for deletion." });
        }

        var isDeleted = rewardsService.DeleteRewards(selectedRewards);
        if (isDeleted)
        {
            return Json(new { success = true, message = "Selected rewards deleted successfully." });
        }

        return Json(new { success = false, message = "Failed to delete selected rewards." });
    }


    [HttpGet]
    public IActionResult _RewardsPage(int page = 1, int pageSize = 5, string sortBy = "DiscountRate", string sortOrder = "asc")
    {
        var rewards = rewardsService.GetAllRewards();  // Or fetch the data for the view

        // Filter rewards where Status is "Active" and Quantity is greater than 0
        var rewardDetails = rewards
            .Where(r => r.Status == "Active" && r.Quantity > 0 && r.ValidUntil >= DateTime.Now)
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
            });

        // Apply sorting
        rewardDetails = sortBy switch
        {
            "DiscountRate" => sortOrder == "asc" ? rewardDetails.OrderBy(r => r.DiscountRate) : rewardDetails.OrderByDescending(r => r.DiscountRate),
            "PointsRequired" => sortOrder == "asc" ? rewardDetails.OrderBy(r => r.PointsRequired) : rewardDetails.OrderByDescending(r => r.PointsRequired),
            "Name" => sortOrder == "asc" ? rewardDetails.OrderBy(r => r.Name) : rewardDetails.OrderByDescending(r => r.Name),
            _ => rewardDetails.OrderBy(r => r.DiscountRate),
        };

        // Pagination
        var pagedRewards = rewardDetails
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Get total count of active rewards to calculate the total number of pages
        var totalRewards = rewards
            .Where(r => r.Status == "Active" && r.Quantity > 0)
            .Count();

        // Create a model for pagination info
        ViewData["TotalPages"] = (int)Math.Ceiling((double)totalRewards / pageSize);
        ViewData["CurrentPage"] = page;
        ViewData["SortBy"] = sortBy;
        ViewData["SortOrder"] = sortOrder;

        return PartialView("_RewardsPage", pagedRewards);
    }
}
