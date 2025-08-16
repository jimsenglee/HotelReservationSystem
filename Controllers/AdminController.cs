using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelDataReader;
using HotelRoomReservationSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using X.PagedList.Extensions;
using System.Linq;
using HotelRoomReservationSystem.BLL.Interfaces;


namespace HotelRoomReservationSystem.Controllers
{
    public class AdminController : Controller
    {
        private static readonly object LockObject = new object();
        private readonly HotelRoomReservationDB db;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment en;
        private readonly ILogger<AccountController> _logger;
        private readonly Helper hp;
        private readonly IMembershipService membershipService;
        private readonly IMembershipRewardsService membershipRewardsService;
        private readonly IRewardsService rewardsService;

        public AdminController(
        IConfiguration configuration, IWebHostEnvironment en, HotelRoomReservationDB db, Helper hp
            , IMembershipService membershipService, IMembershipRewardsService membershipRewardsService
        , IRewardsService rewardsService)
        {
            this.db = db;
            //_logger = logger;
            _configuration = configuration;
            this.hp = hp;
            this.en = en;
            this.membershipService = membershipService ?? throw new ArgumentNullException(nameof(membershipService));
            this.membershipRewardsService = membershipRewardsService;
            this.rewardsService = rewardsService;
        }

        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Index()
        {
            var today = DateTime.Today;


            // Query transactions for today
            var todaysTransactions = db.Transaction
                .Include(t => t.Users) // Optional
                .Include(t => t.Reservation) // Optional
                .Where(t => t.PaymentDate.HasValue && t.PaymentDate.Value.Date == today)
                .ToList();

            var todaysReservationCount = db.Reservation
                .Include(t => t.Users) // Optional
                .Include(t => t.Room) // Optional
                .Where(t => t.DateCreated.Date == today) // Filter by today's CheckInDate
                .Count(); // Get the count of reservations


            var todaysTotalRevenue = todaysTransactions.Sum(t => t.Amount);

            var model = new DashboardViewModel
            {
                TotalRevenue = todaysTotalRevenue,
                TotalReservation = todaysReservationCount,
                weekLabels = GetMonthWeekRanges(),
                //Sales
                weeklySalesData = WeeklySalesData(),
                monthlySalesData = MonthlySalesData(),
                //Bookings
                weeklyBookingsData = WeeklyBookingsData(),
                monthlyBookingsData = MonthlyBookingsData(),
                //Other
                moneyEarnInEachTime = MoneyEarnInEachTime(),
                reservationEarnInEachTime = ReserveInEachTime(),

                //Occupancy
                GetDailyOccupancyRates = GetDailyOccupancyRates(DateTime.Now.Month, DateTime.Now.Year),

                //Top Sell
                categoryList = GetCategoryList(),
                topSellingCategory = CountTopSellingRooms(),

                //Feedback
                feedbacks = GetFeedbacks()
            };

            return View("Dashboard", model);
        }

        //Feedback
        [Authorize(Roles = "Admin,Manager")]
        public List<Feedback> GetFeedbacks()
        {
            var today = DateTime.Today; // Get the current date (midnight of today)

            // Query feedbacks created today
            var todaysFeedbacks = db.Feedback
                .Include(f => f.Users) // Include user details, if necessary
                .Include(f => f.Reservation) // Include reservation details, if necessary
                .Where(f => f.DateCreated.Date == today) // Filter by today's date
                .ToList();

            return todaysFeedbacks;
        }
        //Other
        [Authorize(Roles = "Admin,Manager")]
        public List<decimal> MoneyEarnInEachTime()
        {
            // Get today's date in real-time world
            var today = DateTime.Today;

            // Query transactions for today (assuming Transaction has PaymentDate and Amount)
            var todaysTransactions = db.Transaction
                .Where(t => t.PaymentDate.HasValue && t.PaymentDate.Value.Date == today) // Filter by today's date
                .ToList();

            // Group by hour and calculate the total money for each hour (ignoring minutes and seconds)
            var moneyGroupedByHour = todaysTransactions
                .GroupBy(t => t.PaymentDate.Value.Hour) // Group by hour
                .Select(g => new
                {
                    Hour = g.Key,
                    TotalMoney = g.Sum(t => t.Amount) // Sum the amount for each hour
                })
                .OrderBy(g => g.Hour) // Sort by hour
                .ToList();

            // Initialize a list with 24 elements, all set to 0
            var moneyPerHour = new List<decimal>(new decimal[24]);

            // Fill the moneyPerHour list with summed amounts for the corresponding hours
            foreach (var item in moneyGroupedByHour)
            {
                moneyPerHour[item.Hour] = item.TotalMoney; // Assign the total amount to the correct hour
            }

            return moneyPerHour;
        }

        [Authorize(Roles = "Admin,Manager")]
        public List<int> ReserveInEachTime()
        {
            // Get today's date in real-time world
            var today = DateTime.Today;

            // Query reservations for today (assuming Reservation has CheckInDate)
            var todaysReservations = db.Reservation
                .Where(r => r.DateCreated.Date == today) // Filter by today's CheckInDate
                .ToList();

            // Group by hour of CheckInDate and count the number of reservations for each hour
            var reservationsGroupedByHour = todaysReservations
                .GroupBy(r => r.DateCreated.Hour) // Group by hour of CheckInDate
                .Select(g => new
                {
                    Hour = g.Key,
                    ReservationCount = g.Count() // Count the reservations for each hour
                })
                .OrderBy(g => g.Hour) // Sort by hour
                .ToList();

            // Initialize a list with 24 elements, all set to 0
            var reservationsPerHour = new List<int>(new int[24]);

            // Fill the reservationsPerHour list with counted reservations for the corresponding hours
            foreach (var item in reservationsGroupedByHour)
            {
                reservationsPerHour[item.Hour] = item.ReservationCount; // Assign the reservation count to the correct hour
            }

            return reservationsPerHour;
        }

        //Sales Data
        [Authorize(Roles = "Admin,Manager")]
        public List<decimal> WeeklySalesData()
        {
            // Get the current date and calculate the start of the week (assuming the week starts on Monday)
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);

            // Calculate the end of the week as the upcoming Saturday
            var endOfWeek = startOfWeek.AddDays(6);

            // Query transactions for the current week up to Saturday
            var weeklyTransactions = db.Transaction
                .Where(t => t.PaymentDate.HasValue && t.PaymentDate.Value.Date >= startOfWeek && t.PaymentDate.Value.Date <= endOfWeek) // Filter from Monday to Saturday
                .ToList();

            // Group transactions by day of the week and calculate total revenue for each day
            var revenueGroupedByDay = weeklyTransactions
                .GroupBy(t => t.PaymentDate.Value.DayOfWeek) // Group by DayOfWeek
                .Select(g => new
                {
                    Day = g.Key,
                    TotalRevenue = g.Sum(t => t.Amount) // Sum the amount for each day
                })
                .ToDictionary(g => g.Day, g => g.TotalRevenue);

            // Initialize a list with 7 elements for each day of the week, all set to 0
            var weeklyRevenue = new List<decimal>(new decimal[7]);

            // Fill the list with revenue for the corresponding days
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                if (revenueGroupedByDay.TryGetValue(day, out decimal totalRevenue))
                {
                    weeklyRevenue[(int)day] = totalRevenue; // Assign the total revenue to the correct day
                }
            }

            // Rearrange the list to start with Monday and end with Sunday
            var mondayStartRevenue = new List<decimal>();
            mondayStartRevenue.AddRange(weeklyRevenue.Skip(1)); // Skip Sunday
            mondayStartRevenue.Add(weeklyRevenue[0]); // Add Sunday to the end

            return mondayStartRevenue;
        }

        [Authorize(Roles = "Admin,Manager")]
        public List<decimal> MonthlySalesData()
        {
            // Get the current date and the first and last day of the current month
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Query transactions for the current month
            var monthlyTransactions = db.Transaction
                .Where(t => t.PaymentDate.HasValue && t.PaymentDate.Value.Date >= startOfMonth && t.PaymentDate.Value.Date <= endOfMonth) // Filter by current month
                .ToList();

            // Group transactions by day of the month and calculate total revenue for each day
            var revenueGroupedByDay = monthlyTransactions
                .GroupBy(t => t.PaymentDate.Value.Day) // Group by day of the month
                .Select(g => new
                {
                    Day = g.Key,
                    TotalRevenue = g.Sum(t => t.Amount) // Sum the amount for each day
                })
                .ToDictionary(g => g.Day, g => g.TotalRevenue);

            // Get the number of days in the current month
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

            // Initialize a list with one element for each day of the month, all set to 0
            var dailyRevenue = new List<decimal>(new decimal[daysInMonth]);

            // Fill the list with revenue for the corresponding days
            for (int day = 1; day <= daysInMonth; day++)
            {
                if (revenueGroupedByDay.TryGetValue(day, out decimal totalRevenue))
                {
                    dailyRevenue[day - 1] = totalRevenue; // Assign the total revenue to the correct day (0-based index)
                }
            }

            // Now group the daily revenue into weeks (7 days per week)
            var weeksRevenue = new List<decimal>();
            for (int i = 0; i < dailyRevenue.Count; i += 7)
            {
                // Sum the revenue for the current week (up to 7 days)
                var weekRevenue = dailyRevenue.Skip(i).Take(7).Sum();
                weeksRevenue.Add(weekRevenue);
            }

            return weeksRevenue;
        }

        //Bookings Data
        [Authorize(Roles = "Admin,Manager")]
        public List<int> WeeklyBookingsData()
        {
            // Get the current date and calculate the start of the week (assuming the week starts on Monday)
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);

            // Calculate the end of the week as the upcoming Saturday
            var endOfWeek = startOfWeek.AddDays(6);

            // Query reservations for the current week (Monday to Saturday)
            var weeklyReservations = db.Reservation
                .Where(r => r.DateCreated.Date >= startOfWeek && r.DateCreated.Date <= endOfWeek) // Filter by DateCreated
                .ToList();

            // Group reservations by day of the week and count the number of reservations for each day
            var reservationsGroupedByDay = weeklyReservations
                .GroupBy(r => r.DateCreated.DayOfWeek) // Group by DayOfWeek
                .Select(g => new
                {
                    Day = g.Key,
                    ReservationCount = g.Count() // Count the number of reservations for each day
                })
                .ToDictionary(g => g.Day, g => g.ReservationCount);

            // Initialize a list with 7 elements for each day of the week, all set to 0
            var weeklyBookings = new List<int>(new int[7]);

            // Fill the list with booking counts for the corresponding days
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                if (reservationsGroupedByDay.TryGetValue(day, out int reservationCount))
                {
                    weeklyBookings[(int)day] = reservationCount; // Assign the reservation count to the correct day
                }
            }

            // Rearrange the list to start with Monday and end with Sunday
            var mondayStartBookings = new List<int>();
            mondayStartBookings.AddRange(weeklyBookings.Skip(1)); // Skip Sunday
            mondayStartBookings.Add(weeklyBookings[0]); // Add Sunday to the end

            return mondayStartBookings;
        }

        [Authorize(Roles = "Admin,Manager")]
        public List<int> MonthlyBookingsData()
        {
            // Get the current date and the first and last day of the current month
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Query reservations for the current month
            var monthlyReservations = db.Reservation
                .Where(r => r.DateCreated.Date >= startOfMonth && r.DateCreated.Date <= endOfMonth) // Filter by current month
                .ToList();

            // Group reservations by day of the month and calculate the number of reservations for each day
            var reservationsGroupedByDay = monthlyReservations
                .GroupBy(r => r.DateCreated.Day) // Group by day of the month
                .Select(g => new
                {
                    Day = g.Key,
                    ReservationCount = g.Count() // Count the number of reservations for each day
                })
                .ToDictionary(g => g.Day, g => g.ReservationCount);

            // Get the number of days in the current month
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

            // Initialize a list with one element for each day of the month, all set to 0
            var dailyBookings = new List<int>(new int[daysInMonth]);

            // Fill the list with booking counts for the corresponding days
            for (int day = 1; day <= daysInMonth; day++)
            {
                if (reservationsGroupedByDay.TryGetValue(day, out int reservationCount))
                {
                    dailyBookings[day - 1] = reservationCount; // Assign the reservation count to the correct day (0-based index)
                }
            }

            // Now group the daily bookings into weeks (7 days per week)
            var weeksBookings = new List<int>();
            for (int i = 0; i < dailyBookings.Count; i += 7)
            {
                // Sum the bookings for the current week (up to 7 days)
                var weekBookings = dailyBookings.Skip(i).Take(7).Sum();
                weeksBookings.Add(weekBookings);
            }

            return weeksBookings;
        }

        //Occupancy
        [Authorize(Roles = "Admin,Manager")]
        public List<decimal> GetDailyOccupancyRates(int month, int year)
        {
            var dailyOccupancyRates = new List<decimal>();

            // Get the total number of rooms available for each day of the month
            var totalRoomsInMonth = db.Rooms
                .Where(r => r.RoomType.Status == "Available") // Filter rooms that are marked as available
                .Sum(r => r.RoomType.Quantity); // Sum the quantity of rooms in each category

            // Get the number of days in the month
            var daysInMonth = DateTime.DaysInMonth(year, month);

            // Loop through each day of the month
            for (int day = 1; day <= daysInMonth; day++)
            {
                var currentDate = new DateTime(year, month, day);

                // Get the number of rooms occupied on the current day
                var occupiedRooms = db.Reservation
                    .Where(r => r.CheckInDate <= currentDate && r.CheckOutDate >= currentDate)
                    .Count(); // Count the number of reservations that overlap with this day

                // Calculate the occupancy rate for this day
                decimal occupancyRate = 0;

                if (totalRoomsInMonth > 0)
                {
                    occupancyRate = ((decimal)occupiedRooms / totalRoomsInMonth) * 100;
                }

                // Ensure zero is added for days with no reservations
                dailyOccupancyRates.Add(occupancyRate);
            }

            // Ensure every day of the month is accounted for, even if no data for a specific day
            return dailyOccupancyRates;
        }

        //Week
        [Authorize(Roles = "Admin,Manager")]
        public List<string> GetMonthWeekRanges()
        {
            // Get the current date and the first and last day of the current month
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // List to store the week ranges (e.g., "1-7", "8-14")
            var weekRanges = new List<string>();

            // Initialize the start day for the first week
            var startDay = startOfMonth.Day;

            // Iterate through the days in the month in increments of 7
            for (int i = startDay; i <= endOfMonth.Day; i += 7)
            {
                // Calculate the start and end day for the current week
                var currentStartDay = i;
                var currentEndDay = Math.Min(i + 6, endOfMonth.Day); // Make sure we don't go past the last day of the month

                // Format the week range as a string (e.g., "1-7")
                weekRanges.Add($"{currentStartDay}-{currentEndDay}");
            }

            return weekRanges;
        }

        //Top Selling Rooms
        [Authorize(Roles = "Admin,Manager")]
        public List<string> GetCategoryList()
        {
            // Fetch category names from the database
            var categoryNames = db.RoomType
                .OrderBy(c => c.Id) // Ensures consistent ordering
                .Select(c => c.Name)
                .ToList();

            return categoryNames;
        }

        [Authorize(Roles = "Admin,Manager")]
        public List<int> CountTopSellingRooms()
        {
            // Initialize the list to store the count of reservations per category
            var categoryBookingCounts = new List<int>();

            // Fetch all categories ordered by their Id to match the sequence of GetCategoryList()
            var categories = db.RoomType
                .OrderBy(c => c.Id)
                .ToList();

            foreach (var category in categories)
            {
                // Count the number of reservations for rooms in this category
                int bookingCount = db.Reservation
                    .Include(r => r.Room) // Ensure Room navigation property is loaded
                    .Where(r => r.Room.Id == category.Id) // Filter by rooms in the current category
                    .Count();

                categoryBookingCounts.Add(bookingCount);
            }

            return categoryBookingCounts;
        }

        [Authorize(Roles = "Admin,Manager")]
        //Combination Of Charts
        public IActionResult CombinationOfCharts()
        {
            return View();
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public JsonResult GenerateCharts(string chartType1, string? chartType2, string dataType1, string? dataType2, string startDate, string endDate)
        {
            // Trim and check for empty or whitespace values
            if (string.IsNullOrWhiteSpace(chartType1) ||
                string.IsNullOrWhiteSpace(dataType1) ||
                string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
            {
                return Json(new { success = false, message = "All fields must be selected and cannot be empty or blank." });
            }

            try
            {
                // Parse the start and end dates
                DateTime start = DateTime.Parse(startDate);
                DateTime end = DateTime.Parse(endDate);

                // Validate that end date is not before start date
                if (end < start)
                {
                    return Json(new { success = false, message = "End date cannot be earlier than start date." });
                }

                // Prepare data and labels
                List<decimal> data1 = new List<decimal>();
                List<string> labels = Enumerable.Range(0, (end - start).Days + 1)
                                       .Select(offset => start.AddDays(offset).ToString("dd-MM-yyyy"))
                                       .ToList();
                List<decimal> data2 = new List<decimal>();

                // Process data for the first data type
                if (dataType1 == "revenue")
                {
                    data1 = GetTransactionAmountsByDateRange(start, end);
                }
                else if (dataType1 == "reservation")
                {
                    var reservationCounts = GetReservationCountsByDateRange(start, end);
                    data1 = reservationCounts.Select(c => (decimal)c).ToList();
                }
                else if (dataType1 == "user")
                {
                    var userJoinCounts = GetUserCountsByDateRange(start, end);
                    data1 = userJoinCounts.Select(c => (decimal)c).ToList();
                }
                else if (dataType1 == "membership")
                {
                    var membershipCounts = GetMembershipCountsByDateRange(start, end);
                    data1 = membershipCounts.Select(c => (decimal)c).ToList();
                }
                else
                {
                    return Json(new { success = false, message = "Invalid data type selected." });
                }

                // Check if second chart type and data type are provided
                if (!string.IsNullOrEmpty(chartType2) && !string.IsNullOrEmpty(dataType2))
                {
                    // Process data for the second data type
                    if (dataType2 == "revenue")
                    {
                        data2 = GetTransactionAmountsByDateRange(start, end);
                    }
                    else if (dataType2 == "reservation")
                    {
                        var reservationCounts = GetReservationCountsByDateRange(start, end);
                        data2 = reservationCounts.Select(c => (decimal)c).ToList();
                    }
                    else if (dataType2 == "user")
                    {
                        var userJoinCounts = GetUserCountsByDateRange(start, end);
                        data2 = userJoinCounts.Select(c => (decimal)c).ToList();
                    }
                    else if (dataType2 == "membership")
                    {
                        var membershipCounts = GetMembershipCountsByDateRange(start, end);
                        data2 = membershipCounts.Select(c => (decimal)c).ToList();
                    }
                    else
                    {
                        return Json(new { success = false, message = "Invalid second data type selected." });
                    }
                }

                // Return data for both charts
                return new JsonResult(new
                {
                    success = true,
                    data1 = data1,
                    data2 = data2,
                    labels = labels
                });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        [Authorize(Roles = "Admin,Manager")]
        // Helper Function: Get transactions amounts by date range
        private List<decimal> GetTransactionAmountsByDateRange(DateTime startDate, DateTime endDate)
        {
            List<decimal> transactionTotals = new List<decimal>();

            for (DateTime currentDate = startDate; currentDate <= endDate; currentDate = currentDate.AddDays(1))
            {
                decimal dailyTotal = db.Transaction
                    .Where(t => t.PaymentDate.HasValue && t.PaymentDate.Value.Date == currentDate.Date)
                    .Sum(t => t.Amount);

                transactionTotals.Add(dailyTotal);
            }

            return transactionTotals;
        }

        [Authorize(Roles = "Admin,Manager")]
        // Helper Function: Get reservation counts by date range
        private List<int> GetReservationCountsByDateRange(DateTime startDate, DateTime endDate)
        {
            List<int> reservationCounts = new List<int>();

            for (DateTime currentDate = startDate; currentDate <= endDate; currentDate = currentDate.AddDays(1))
            {
                // Count reservations created on the current date
                int dailyCount = db.Reservation
                    .Where(r => r.DateCreated.Date == currentDate.Date)  // Compare DateCreated field
                    .Count();

                reservationCounts.Add(dailyCount);
            }

            return reservationCounts;
        }

        [Authorize(Roles = "Admin,Manager")]
        // Helper Function: Get user counts by join date range
        private List<int> GetUserCountsByDateRange(DateTime startDate, DateTime endDate)
        {
            List<int> userCounts = new List<int>();

            for (DateTime currentDate = startDate; currentDate <= endDate; currentDate = currentDate.AddDays(1))
            {
                // Count users who joined on the current date
                int dailyCount = db.Users
                    .Where(u => u.DateCreated.Date == currentDate.Date)  // Compare DateCreated field
                    .Count();

                userCounts.Add(dailyCount);
            }

            return userCounts;
        }

        [Authorize(Roles = "Admin,Manager")]
        // Helper Function: Get membership counts by join date range
        private List<int> GetMembershipCountsByDateRange(DateTime startDate, DateTime endDate)
        {
            List<int> membershipCounts = new List<int>();

            for (DateTime currentDate = startDate; currentDate <= endDate; currentDate = currentDate.AddDays(1))
            {
                // Count memberships that started on the current date
                int dailyCount = db.Memberships
                    .Where(m => m.LastCheckinDate == currentDate.Date)  // Compare StartDate field
                    .Count();

                membershipCounts.Add(dailyCount);
            }

            return membershipCounts;
        }

        [Authorize(Roles = "Admin,Manager")]
        //Compact of Data
        public IActionResult CompactOfData()
        {
            return View();
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public JsonResult GenerateData(string chartType, string dataType, string startDate1, string endDate1, string startDate2, string endDate2)
        {
            // Trim and check for empty or whitespace values
            if (string.IsNullOrWhiteSpace(chartType) || string.IsNullOrWhiteSpace(dataType) ||
                string.IsNullOrWhiteSpace(startDate1) || string.IsNullOrWhiteSpace(endDate1) ||
                string.IsNullOrWhiteSpace(startDate2) || string.IsNullOrWhiteSpace(endDate2))
            {
                return Json(new { success = false, message = "All fields must be selected and cannot be empty or blank." });
            }

            try
            {
                // Parse the start and end dates for both ranges
                DateTime start1 = DateTime.Parse(startDate1);
                DateTime end1 = DateTime.Parse(endDate1);
                DateTime start2 = DateTime.Parse(startDate2);
                DateTime end2 = DateTime.Parse(endDate2);

                // Validate that end dates are not before start dates
                if (end1 < start1)
                {
                    return Json(new { success = false, message = "End date for Range 1 cannot be earlier than start date." });
                }
                if (end2 < start2)
                {
                    return Json(new { success = false, message = "End date for Range 2 cannot be earlier than start date." });
                }

                // Validate that the two date ranges do not overlap or match
                if ((start1 <= end2 && end1 >= start2) || (start1 == start2 && end1 == end2))
                {
                    return Json(new { success = false, message = "The two date ranges must not overlap or match." });
                }


                // Prepare data and labels for Range 1
                List<decimal> data1 = new List<decimal>();
                List<string> labels1 = new List<string>();

                if (dataType == "revenue")
                {
                    data1 = GetTransactionAmountsByDateRange(start1, end1);
                    labels1 = Enumerable.Range(0, (end1 - start1).Days + 1)
                                        .Select(offset => start1.AddDays(offset).ToString("dd-MM-yyyy"))
                                        .ToList();
                }
                else if (dataType == "reservation")
                {
                    var reservationCounts = GetReservationCountsByDateRange(start1, end1);
                    data1 = reservationCounts.Select(c => (decimal)c).ToList();
                    labels1 = Enumerable.Range(0, (end1 - start1).Days + 1)
                                        .Select(offset => start1.AddDays(offset).ToString("dd-MM-yyyy"))
                                        .ToList();
                }
                else if (dataType == "user")
                {
                    var userJoinCounts = GetUserCountsByDateRange(start1, end1);
                    data1 = userJoinCounts.Select(c => (decimal)c).ToList();
                    labels1 = Enumerable.Range(0, (end1 - start1).Days + 1)
                                        .Select(offset => start1.AddDays(offset).ToString("dd-MM-yyyy"))
                                        .ToList();
                }
                else if (dataType == "membership")
                {
                    var membershipCounts = GetMembershipCountsByDateRange(start1, end1);
                    data1 = membershipCounts.Select(c => (decimal)c).ToList();
                    labels1 = Enumerable.Range(0, (end1 - start1).Days + 1)
                                        .Select(offset => start1.AddDays(offset).ToString("dd-MM-yyyy"))
                                        .ToList();
                }
                else
                {
                    return Json(new { success = false, message = "Invalid data type selected for Range 1." });
                }

                // Prepare data and labels for Range 2
                List<decimal> data2 = new List<decimal>();
                List<string> labels2 = new List<string>();

                // Fetch data for Range 2 based on the same dataType
                if (dataType == "revenue")
                {
                    data2 = GetTransactionAmountsByDateRange(start2, end2);
                    labels2 = Enumerable.Range(0, (end2 - start2).Days + 1)
                                        .Select(offset => start2.AddDays(offset).ToString("dd-MM-yyyy"))
                                        .ToList();
                }
                else if (dataType == "reservation")
                {
                    var reservationCounts = GetReservationCountsByDateRange(start2, end2);
                    data2 = reservationCounts.Select(c => (decimal)c).ToList();
                    labels2 = Enumerable.Range(0, (end2 - start2).Days + 1)
                                        .Select(offset => start2.AddDays(offset).ToString("dd-MM-yyyy"))
                                        .ToList();
                }
                else if (dataType == "user")
                {
                    var userJoinCounts = GetUserCountsByDateRange(start2, end2);
                    data2 = userJoinCounts.Select(c => (decimal)c).ToList();
                    labels2 = Enumerable.Range(0, (end2 - start2).Days + 1)
                                        .Select(offset => start2.AddDays(offset).ToString("dd-MM-yyyy"))
                                        .ToList();
                }
                else if (dataType == "membership")
                {
                    var membershipCounts = GetMembershipCountsByDateRange(start2, end2);
                    data2 = membershipCounts.Select(c => (decimal)c).ToList();
                    labels2 = Enumerable.Range(0, (end2 - start2).Days + 1)
                                        .Select(offset => start2.AddDays(offset).ToString("dd-MM-yyyy"))
                                        .ToList();
                }
                else
                {
                    return Json(new { success = false, message = "Invalid data type selected for Range 2." });
                }


                // Calculate totals for both datasets
                decimal totalData1 = data1.Sum();
                decimal totalData2 = data2.Sum();

                // Combine start and end dates into a single list
                List<string> combinedDateRanges = new List<string>
                {
                $"{start1:dd-MM-yyyy} - {end1:dd-MM-yyyy}",
                $"{start2:dd-MM-yyyy} - {end2:dd-MM-yyyy}"
                };

                return Json(new
                {
                    success = true,
                    data1 = data1,
                    data2 = data2,
                    labels1 = labels1,
                    labels2 = labels2,
                    totalData1 = totalData1,
                    totalData2 = totalData2,
                    combinedDateRanges = combinedDateRanges
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }













        //ABOVE FOR REPORT 
        //
        //


























        //DISPLAY ADMIN AND USER PURPOSE
        //MANAGE VIEW ADMIN AND CUSTOMER
        //ADMIN VIEW CUSTOMER

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult UserManagement(string? searchBar, string? sort, string? dir, int page = 1, int pageSize = 5, string status = "All",string role = "All")
        {
            ViewBag.Name = searchBar = searchBar?.Trim() ?? "";
            ViewBag.PageSize = pageSize;
            ViewBag.Page = page;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;
            ViewBag.Role = role;
            var users = string.IsNullOrEmpty(searchBar)
            ? GetAllUser()
            : GetUsersById(searchBar);

            var userDetails = users.Select(user => new UserDetailVM
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNum = user.PhoneNum,
                DateCreated = user.DateCreated,
                DOB = user.DOB,
                Portrait = user.Portrait,
                Status = user.Status,
                Role = user.Role,
            }).ToList();

            if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                userDetails = userDetails
                    .Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            dir = dir?.ToLower() == "des" ? "des" : "asc";
            if (!string.Equals(role, "All", StringComparison.OrdinalIgnoreCase))
            {
                userDetails = userDetails
                    .Where(r => string.Equals(r.Role, role, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            dir = dir?.ToLower() == "des" ? "des" : "asc";

            Func<UserDetailVM, object> fn = sort switch
            {
                "UserId" => r => r.Id,
                "Name" => r => r.Name,
                "Email" => r => r.Email,
                "PhoneNum" => r => r.PhoneNum,
                "DateCreated" => r => r.DateCreated,
                "DOB" => r => r.DOB,
                "Portrait" => r => r.Portrait,
                "Roles" => r => r.Role,
                "Status" => r => r.Status,
                _ => r => r.Id // Default sorting column
            };

            var sorted = dir == "des" ? userDetails.OrderByDescending(fn) : userDetails.OrderBy(fn);
            if (pageSize == 0) 
            {
                pageSize = users.Count();
            }

            if (page < 1)
            {
                return RedirectToAction(null, new { searchBar, sort, dir, page = 1, pageSize, status });
            }

            var m = sorted.ToPagedList(page, pageSize); 

            if (page > m.PageCount && m.PageCount > 0) 
            {
                return RedirectToAction(null, new { searchBar, sort, dir, page = m.PageCount, pageSize, status });
            }

            if (Request.IsAjax())
            {
                return PartialView("_UserList", m); // Return only the updated reservation list and pagination controls
            }

            return View("UserManagement",m);
        }

        [Authorize(Roles = "Admin,Manager")]
        public List<HotelRoomReservationSystem.Models.Users> GetAllUser()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "Manager")
            {
                return db.Users
                   .AsEnumerable() 
                   .Where(u => u.Role != "Manager")
                   .ToList();
            }
            return db.Users
            
             .AsEnumerable() 
             .Where(u => u.Role == "Customer")
             .ToList();
        }

        [Authorize(Roles = "Admin,Manager")]
        public List<HotelRoomReservationSystem.Models.Users> GetUsersById(string searchBar)
        {
        //    return db.Users
        //             .Where(u => (u.Name.Contains(searchBar) || u.Id.ToString().Contains(searchBar))
        //                          && u.Status != "Deleted")
        //             .ToList();

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "Manager")
            {
                return db.Users
                     .Where(u => (u.Name.Contains(searchBar) || u.Id.ToString().Contains(searchBar)))
                     .AsEnumerable()
                     .Where(u => u.Role != "Manager")
                     .ToList();

            }
            return db.Users
                     .Where(u => (u.Name.Contains(searchBar) || u.Id.ToString().Contains(searchBar)))
                     .AsEnumerable()
                     .Where(u => u.Role == "Customer")
                     .ToList();

        }

        [Authorize(Roles = "Admin,Manager")]
        public HotelRoomReservationSystem.Models.Users GetUserById(string userId)
        {
            var user = db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found or is deleted.");
            }
            return user;
        }

        //DISPLAY ADMIN AND USER PURPOSE
        //MANAGE VIEW ADMIN AND CUSTOMER
        //ADMIN VIEW CUSTOMER


        //UPDATE USER STATUS
        //BLOCK,VERIFY AND PENDING 
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Status))
            {
                Console.WriteLine($"Invalid request - ID: {request.Id}, Status: {request.Status}");
                return Json(new { success = false, message = "Invalid request." });
            }

            // Retrieve the user
            var user = GetUserById(request.Id);
            if (user == null)
            {
                Console.WriteLine($"User not found - ID: {request.Id}");
                return Json(new { success = false, message = "User not found." });
            }

            string currentStatus = user.Status;

            // Valid status transitions
            var validTransitions = new Dictionary<string, string>
            {
                { "PENDING", "VERIFY" },
                   { "VERIFY", "BLOCK" },
                {"BLOCK","VERIFY" },
                 {"DELETED","VERIFY" },
                {"REQUESTED","RECOVERING" }
                };
        
            // Check if the requested status transition is valid
            if (!validTransitions.TryGetValue(currentStatus, out var allowedNextStatus) || allowedNextStatus != request.Status)
            {
                return Json(new { success = false, message = "Invalid status transition." });
            }

            // Update the status
            user.Status = request.Status;
            UpdateUserStatus(user);
            if (request.Status == "RECOVERING")
            {
                HandleTokenGeneration(user.Id,"RECOVER");
            }

            return Json(new { success = true });
        }

        [Authorize(Roles = "Admin,Manager")]
        public  Task HandleTokenGeneration(string userId, string purpose)
        {
           
            var user = db.Users
                .Where(u => u.Id == userId)
                .FirstOrDefault();

            if (user != null)
            {
                string newToken = Guid.NewGuid().ToString();
                DateTime expiryDate = DateTime.Now.AddDays(2);

                var tokenEntity = new HotelRoomReservationSystem.Models.Token
                {
                    token = newToken,
                    Expiration = expiryDate,
                    UsersId = userId,
                    Purpose = purpose
                };

                db.Token.Add(tokenEntity);
                db.SaveChanges();

                string verificationUrl = Url.Action("VerifyEmail", "Account", new { token = newToken }, Request.Scheme);

                StringBuilder mailBody = new StringBuilder();
                mailBody.Append("<html><head></head><body>");
                mailBody.Append("<p>Dear User,</p>");
                mailBody.Append($"<p>Please click <a href='{verificationUrl}'>here</a> to activate for recover your account.</p>");
                mailBody.Append("<p>Note: This link is valid for 2 days only.</p>");
                mailBody.Append("<p>Regards,</p>");
                mailBody.Append("<p>Hotel Team</p>");
                mailBody.Append("</body></html>");
                bool emailSent = SendEmail("yeapzijia1936@gmail.com", user.Email, "Email Verification - Recover Account", mailBody.ToString());

                if (emailSent)
                {
                    //TempData["Message"] = "Verification email sent successfully. Please check your email.";
                    //TempData["IsSuccess"] = true;
                }
                else
                {
                    //TempData["Message"] = "Failed to send verification email.";
                    //TempData["IsSuccess"] = false;
                }
            }
            else
            {
                //TempData["Message"] = "No users found in the database.";
                //TempData["IsSuccess"] = false;
            }
            return Task.CompletedTask;
        }

       
        public bool SendEmail(string mailFrom, string Tomail, string sub, string body)
        {
            if (string.IsNullOrEmpty(mailFrom) || string.IsNullOrEmpty(Tomail))
            {
                Console.WriteLine("Invalid email address provided.");
                return false;
            }

            using (MailMessage mail = new MailMessage())
            {
                string displayName = _configuration["AppSettings:DisplayName"];
                mailFrom = _configuration["AppSettings:smtpUser"];

                mail.To.Add(Tomail.Trim());
                mail.From = new MailAddress(mailFrom, displayName);
                mail.Subject = sub.Trim();
                mail.Body = body.Trim();
                mail.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient())
                {
                    smtp.Host = _configuration["AppSettings:smtpServer"];
                    smtp.Port = Convert.ToInt32(_configuration["AppSettings:smtpPort"]);
                    smtp.EnableSsl = Convert.ToBoolean(_configuration["AppSettings:EnableSsl"]);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(
                        _configuration["AppSettings:smtpUser"],
                        _configuration["AppSettings:PWD"]
                    );

                    smtp.Timeout = 20000;
                    smtp.Send(mail);
                    Console.WriteLine("Email sent successfully.");
                    return true;
                }
                return false;
            }

        }

        [Authorize(Roles = "Admin,Manager")]
        public bool UpdateUserStatus(HotelRoomReservationSystem.Models.Users user)
        {
            if (user == null || user.Id == null)
                return false;

            // Delegate the actual update to the repository
            return Update(user);
        }

        [Authorize(Roles = "Admin,Manager")]
        public bool Update(HotelRoomReservationSystem.Models.Users user)
        {
            var existingUser = db.Users.FirstOrDefault(rt => rt.Id == user.Id);
            if (existingUser != null)
            {
                existingUser.Status = user.Status;

                db.SaveChanges();
               
                return true;
            }
            return false;
        }
        //UPDATE USER STATUS
        //BLOCK,VERIFY AND PENDING 


        //BATCH UPDATE
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult BatchUpdateStatuses([FromBody] List<string> userIds)
        {
            if (userIds == null || userIds.Count == 0)
            {
                Console.WriteLine($"Invalid request - userIds: {string.Join(", ", userIds)}");
                return Json(new { success = false, message = "Invalid request." });
            }

            var users = GetUsersByIds(userIds); // Assume this method retrieves the users from the database
            if (users == null || users.Count == 0)
            {
                Console.WriteLine($"Users not found - IDs: {string.Join(", ", userIds)}");
                return Json(new { success = false, message = "Users not found." });
            }

            var validTransitions = new Dictionary<string, string>
    {
        { "PENDING", "VERIFY" },
        { "VERIFY", "BLOCK" },
        { "BLOCK", "VERIFY" },
                {"DELETED","VERIFY" },
                 {"REQUESTED","RECOVERING" }
    };

            foreach (var user in users)
            {
                string currentStatus = user.Status;
                if (!validTransitions.TryGetValue(currentStatus, out var allowedNextStatus))
                {
                    return Json(new { success = false, message = $"Invalid status transition for user ID: {user.Id}" });
                }

                // Update the status to the valid next status
                user.Status = allowedNextStatus;
            }

            UpdateUsersStatus(users);
            return Json(new { success = true, message = $"{users.Count} users successfully updated." });
        }

        private bool UpdateUsersStatus(List<HotelRoomReservationSystem.Models.Users>  users)
        {
            try
            {
                foreach (var user in users)
                {
                    db.Users.Update(user);  // Update each user's status
                    if (user.Status == "RECOVERING")
                    {
                        HandleTokenGeneration(user.Id, "RECOVER");
                    }
                }

                db.SaveChanges();  // Commit the changes to the database
               
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating users: {ex.Message}");
                return false;
            }
        }

        private List<HotelRoomReservationSystem.Models.Users> GetUsersByIds(List<string> userIds)
        {
                return db.Users
                              .Where(u => userIds.Contains(u.Id))
                              .ToList();
        }

        //BATCH UPDATE


        //ADD CUSTOMER OR ADMIN
        [Authorize(Roles = "Manager,Admin")]
        public IActionResult AddUserForm()
        {
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            // Determine what roles to show in the dropdown
            List<string> rolesList = new List<string> { "Customer" }; // Default role
            if (userRoles.Contains("Manager"))
            {
                rolesList.Add("Admin");
            }

            ViewBag.RolesList = rolesList;
            return View("AddUserForm");
        }

        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> AddUserForm(AddUsersVM model)
        {
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
            
            List<string> rolesList = new List<string> { "Customer" }; 
            if (userRoles.Contains("Manager"))
            {
                rolesList.Add("Admin");
            }
            ViewBag.RolesList = rolesList;

            foreach (var error in ModelState)
            {
                Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
            }
            Console.WriteLine("Testing");
            Console.WriteLine(model.roles);
            bool hasErrors = false;
            if (hp.CheckEmailExist(model.Email))
            {
                ModelState.AddModelError("Email", "This Email has been registered. Please try another.");
                hasErrors = true;
            }
            if (hp.CheckPhoneExist(model.PhoneNum))
            {
                ModelState.AddModelError("PhoneNum", "This Phone Number has been registered. Please try another.");
                hasErrors = true;
            }
           
            if (hasErrors)
            {
                return View(model);
            }

            if (model.RecaptchaToken == null)
            {
                ModelState.AddModelError("RecaptchaToken", "Captcha is required.");
                return View(model);
            }
            

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            else
            {
                string userId = await hp.GenerateUserIdAsync();
                if(model.roles == "Customer")
                {
                    var customer = new Customer
                    {
                        Id = userId,
                        Name = model.FirstName + " " + model.LastName,
                        Email = model.Email,
                        Password = hp.HashPassword(model.Password),
                        PhoneNum = model.PhoneNum,
                        DateCreated = DateTime.Now,
                        DOB = model.BirthDay,
                        Portrait = "default_profile.jpg",
                        Status = "VERIFY"   
                    };
                    db.Users.Add(customer);
                    membershipService.AddNewMember(userId);
                }
                else if(model.roles == "Admin")
                {
                    var admin = new Admin
                    {
                        Id = userId,
                        Name = model.FirstName + " " + model.LastName,
                        Email = model.Email,
                        Password = hp.HashPassword(model.Password),
                        PhoneNum = model.PhoneNum,
                        DateCreated = DateTime.Now,
                        DOB = model.BirthDay,
                        Portrait = "default_profile.jpg",
                        Status = "VERIFY"
                    };
                    db.Users.Add(admin);
                }

                var mfa = new MFALogin
                {
                    status = "OFF",
                    UsersId = userId,
                    otp = "000000",
                    ExipredDateTime = new DateTime(1, 1, 1)
                };
                db.MFALogin.Add(mfa);

                db.SaveChanges();
               
                TempData["SuccessMessage"] = "User Added successfully!";
                return RedirectToAction("UserManagement");
            }

            
        }
        //ADD CUSTOMER OR ADMIN


        //DELETE CUSTOMER OR ADMIN
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public IActionResult UsersDelete([FromBody] List<string> userIds)
        {
            if (userIds == null)
            {
                return BadRequest(new { message = "No Users selected for deletion." });
            }

            try
            {
                RemoveUsers(userIds);
                return Ok(new { message = $"{userIds.Count} User(s) deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while deleting users.", details = ex.Message });
            }
        }
        [Authorize(Roles = "Manager")]
        public void RemoveUsers(List<string> userIds)
        {
            // Fetch all users whose IDs match the list
            var usersToRemove = db.Users.Where(user => userIds.Contains(user.Id)).ToList();

            if (usersToRemove.Count == 0)
            {
                Console.WriteLine("No users found to remove.");
                return;
            }

            // Update the status for all fetched users
            foreach (var user in usersToRemove)
            {
                user.Status = "DELETED";
            }

            // Save changes in one transaction
            db.SaveChanges();
            Console.WriteLine($"{usersToRemove.Count} users marked as 'Deleted'.");
        }

        //DELETE CUSTOMER OR ADMIN

        //UPDATE ADMIN OR USER BASED ON ID
        [HttpGet]
        [Route("Admin/Details")]
        [Authorize(Roles = "Manager")]
        public IActionResult GetSpecificProfile(string userId)
        {
            var user = db.Users.FirstOrDefault(u => u.Id == userId);
           
            if (user == null) return null;

            var nameParts = user.Name?.Split(' ') ?? Array.Empty<string>();
            var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
            var lastName = string.Join(" ", nameParts.Skip(1));

           

            var userDetails = new UpdateUserVM
            {
                id =user.Id,
                FirstName = firstName,
                LastName = lastName,
                Email = user.Email,
                PhoneNum = user.PhoneNum,
                portrait = user.Portrait,
                BirthDay = user.DOB,
               
            };

            return View("UpdateUserForm", userDetails);
        }

        
        [HttpGet]
        [Authorize(Roles = "Manager")]
        public IActionResult GetSpecificDetails(string userId)
        {
            var user = GetProfile(userId);

            if (user == null)
            {
                return NotFound(new { message = "Users not found" });
            }
            var nameParts = user.Name?.Split(' ') ?? Array.Empty<string>();
            var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
            var lastName = string.Join(" ", nameParts.Skip(1));
            // Map room details to a response object
            var response = new
            {
                FirstName = firstName,
                LastName = lastName,
                Email = user.Email,
                PhoneNum = user.PhoneNum,
                portrait = user.Portrait,
                BirthDay = user.DOB,
            };

            return Json(response);
        }

        public HotelRoomReservationSystem.Models.Users GetProfile(string userId)
        {
            return db.Users.Find(userId);
        }

        [Authorize(Roles = "Manager")]
        public IActionResult UpdateUserForm(UpdateUserVM model)
        {
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public IActionResult UpdateUserForm(UpdateUserVM model,string ID = null)
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var existingUser = db.Users.FirstOrDefault(u => u.Id == model.id);

            if (existingUser == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }

            if (!ValidateUserDetails(model, existingUser))
            {
                model.portrait = existingUser.Portrait;
                PrepareModelForReturn(model, existingUser);
                return View(model);
            }

            bool isUnchanged =
        existingUser.Name == $"{model.FirstName} {model.LastName}" &&
        existingUser.Email == model.Email &&
        existingUser.PhoneNum == model.PhoneNum &&
        existingUser.DOB == model.BirthDay &&
        string.IsNullOrEmpty(model.Base64Photo) &&
        model.Photo == null;

            if (isUnchanged)
            {
                TempData["infoMessage"] = "No changes detected in your profile.";
                return RedirectToAction("UserManagement");
            }

            if (ModelState.IsValid)
            {
                UpdateExistingUser(existingUser, model);
                UpdateUserClaims(existingUser, claimsIdentity);

                db.SaveChanges();

                TempData["SuccessMessage"] = "Profile updated successfully!";

                return RedirectToAction("UserManagement");
            }

            PrepareModelForReturn(model, existingUser);
            return View(model);
        }

        //UPDATE ADMIN OR USER BASED ON ID


        //IMPORT AND EXPORT BASED ON EXCEL
        //UPDATE USING EXCEL
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<FileResult> ExportUserInExcel()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            IEnumerable<HotelRoomReservationSystem.Models.Users> usersEnumerable;

            if (userRole == "Manager")
            {
                usersEnumerable = db.Users
                    .AsEnumerable() // Forces client-side evaluation
                    .Where(u => u.Role != "Manager");
            }
            else
            {
                usersEnumerable = db.Users
                    .AsEnumerable()
                    .Where(u => u.Role == "Customer");
            }

            var users = usersEnumerable.ToList();
            var filename = "user.xlsx";
            return GenerateExcel(filename, users);
        }


        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<FileResult> ExportTargetUserInExcel([FromQuery] string userids)
        {
            if (string.IsNullOrEmpty(userids))
            {
                return null; 
            }

            string cleanedUserIds = userids.Trim('[', ']');
            var ids = cleanedUserIds.Split(',')
                        .Select(id => id.Trim('"')) 
                        .ToList();

            foreach (var id in ids)
            {
                Console.WriteLine(id);
            }
            IEnumerable<HotelRoomReservationSystem.Models.Users> usersEnumerable;

            usersEnumerable = db.Users
            .Where(u => ids.Contains(u.Id))
            .ToList();
            foreach (var user in usersEnumerable)
            {
                Console.WriteLine(user.Id);
            }

            var users = usersEnumerable.ToList();
            var filename = "user.xlsx";
            return GenerateExcel(filename, users);
        }

        private FileResult GenerateExcel(String filename,IEnumerable<HotelRoomReservationSystem.Models.Users> users)
        {
            DataTable dataTable = new DataTable("Users");
            dataTable.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("Id"),
                new DataColumn("Name"),
                new DataColumn("Email"),
                new DataColumn("Password"),
                new DataColumn("Phone Number"),
                new DataColumn("Date Created"),
                new DataColumn("Date Of Birth"),
                new DataColumn("Portrait"),
                new DataColumn("Status"),
                new DataColumn("Roles"),
            });

            foreach(var user in users)
            {
                dataTable.Rows.Add(user.Id, user.Name,user.Email,user.Password,user.PhoneNum,user.DateCreated,user.DOB,user.Portrait,user.Status,user.Role);
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dataTable);
                using(MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);

                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        filename);
                }
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UploadAndUpdateDatabase(IFormFile file)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (file != null && file.Length > 0)
            {
                var uploadFolder = $"{Directory.GetCurrentDirectory()}\\wwwroot\\uploads\\";

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var filePath = Path.Combine(uploadFolder, file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                using (var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        do
                        {
                            bool isHeaderSkipped = false;

                            while (reader.Read())
                            {
                                if (!isHeaderSkipped)
                                {
                                    isHeaderSkipped = true;
                                    continue;
                                }

                                string userId = reader.GetValue(0)?.ToString();
                                if (!IsValidUserId(userId, out JsonResult userIdResult))
                                {
                                    return userIdResult;
                                }

                                var existingUser = db.Users.FirstOrDefault(u => u.Id == userId);

                                string role = reader.GetValue(9)?.ToString();
                                if (!IsValidRole(role, out JsonResult roleResult))
                                {
                                    return roleResult;
                                }
                                string name = reader.GetValue(1)?.ToString();
                                string email = reader.GetValue(2)?.ToString();
                                string password = reader.GetValue(3)?.ToString();
                                string phoneNum = reader.GetValue(4)?.ToString();
                                string dateCreatedStr = reader.GetValue(5)?.ToString();
                                string dobStr = reader.GetValue(6)?.ToString();
                                string portrait = reader.GetValue(7)?.ToString();
                                string status = reader.GetValue(8)?.ToString();
                                if (string.IsNullOrEmpty(name) ||
                                string.IsNullOrEmpty(email) ||
                                string.IsNullOrEmpty(password) ||
                                string.IsNullOrEmpty(phoneNum) ||
                                string.IsNullOrEmpty(dateCreatedStr) ||
                                string.IsNullOrEmpty(dobStr) ||
                                string.IsNullOrEmpty(portrait) ||
                                string.IsNullOrEmpty(status))
                                {
                                    return Json(new { success = false, message = "All fields must be provided and cannot be empty." });
                                }


                                HotelRoomReservationSystem.Models.Users user = existingUser ?? role switch
                                {
                                    "Manager" => new HotelRoomReservationSystem.Models.Manager(),
                                    "Admin" => new HotelRoomReservationSystem.Models.Admin(),
                                    "Customer" => new HotelRoomReservationSystem.Models.Customer(),
                                    _ => new HotelRoomReservationSystem.Models.Users(),
                                };
                                user.Id = userId;
                                user.Name = reader.GetValue(1)?.ToString();
                                user.Email = reader.GetValue(2)?.ToString();
                                if (!IsValidEmail(user.Email, out JsonResult emailResult))  // Validate email using the new method
                                {
                                    return emailResult;
                                }

                                //user.Email = reader.GetValue(2)?.ToString();
                                user.Password = reader.GetValue(3)?.ToString();
                                user.PhoneNum = reader.GetValue(4)?.ToString();

                                if (!IsValidPhoneNumber(user.PhoneNum, out JsonResult phoneNumResult))
                                {
                                    return phoneNumResult;
                                }
                                if (!user.PhoneNum.StartsWith("0"))
                                {
                                    user.PhoneNum = "0" + user.PhoneNum;
                                }


                                string dateCreatedValue = reader.GetValue(5)?.ToString();
                                if (!IsValidDateCreated(dateCreatedValue, out DateTime dateCreated, out JsonResult dateCreatedResult))
                                {
                                    return dateCreatedResult;
                                }
                                user.DateCreated = dateCreated;

                                string dobValue = reader.GetValue(6)?.ToString();
                                if (!IsValidDOB(dobValue, out DateOnly dob, out JsonResult dobResult))
                                {
                                    return dobResult;
                                }
                                user.DOB = dob;


                                user.Portrait = reader.GetValue(7).ToString();
                                user.Status = reader.GetValue(8).ToString();

                                if (existingUser != null && existingUser.Role != role)
                                {
                                    return Json(new { success = false, message = $"Cannot modify user role as it is not allow to edit.." });
                                }

                                if (userRole == "Manager" && role == "Manager")
                                {
                                    return Json(new { success = false, message = "Manager can add or update customer and admin only." });
                                }

                                

                                if (userRole == "Admin" && (role == "Admin"|| role == "Manager"))
                                {
                                    return Json(new { success = false, message = "Admin can add or update customer only." });
                                }

                                bool isEmailDuplicate = db.Users.Any(u => u.Email == user.Email && u.Id != user.Id);
                                bool isPhoneDuplicate = db.Users.Any(u => u.PhoneNum == user.PhoneNum && u.Id != user.Id);

                                if (isEmailDuplicate)
                                {
                                    return Json(new { success = false, message = "Duplicate Email found." });
                                }
                                if (isPhoneDuplicate)
                                {
                                    return Json(new { success = false, message = "Duplicate Phone Number found." });
                                }
                                if (user.Status != "DELETED" && user.Status != "VERIFY" && user.Status != "PENDING" && user.Status != "BLOCK")
                                {
                                    return Json(new { success = false, message = "The status only allow DELETED, VERIFY, PENDING and BLOCK." });
                                }
                                

                                // Insert or update the user
                                if (existingUser == null)
                                {
                                    db.Add(user);
                                }
                                else
                                {
                                    db.Update(user);
                                }

                                await db.SaveChangesAsync();
                            }
                        } while (reader.NextResult());

                    }
                }

                return Json(new { success = true, message = "Database updated successfully!" });
            }
            else
            {
                return Json(new { success = false, message = "No file uploaded or the file is empty." });
            }
        }

        private bool IsValidEmail(string email, out JsonResult result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
            {
                result = Json(new { success = false, message = "Invalid email format." });
                return false;
            }

            return true;
        }
        private bool IsValidUserId(string userId, out JsonResult result)
        {
            if (string.IsNullOrEmpty(userId))
            {
                result = Json(new { success = false, message = "User ID cannot be null or empty." });
                return false;
            }
            result = null;
            return true;
        }

        private bool IsValidRole(string role, out JsonResult result)
        {
            if (string.IsNullOrEmpty(role))
            {
                result = Json(new { success = false, message = "Role cannot be null or empty." });
                return false;
            }
            result = null;
            return true;
        }

        private bool IsValidPhoneNumber(string phoneNum, out JsonResult result)
        {
            result = null; // Initialize result before using it

            if ((phoneNum.StartsWith("011") && phoneNum.Length == 11) ||
                ((phoneNum.StartsWith("010") || phoneNum.StartsWith("012") || phoneNum.StartsWith("013") ||
                phoneNum.StartsWith("014") || phoneNum.StartsWith("015") || phoneNum.StartsWith("016") ||
                phoneNum.StartsWith("017") || phoneNum.StartsWith("018") || phoneNum.StartsWith("019")) && phoneNum.Length == 10) ||
                ((phoneNum.StartsWith("11") && phoneNum.Length == 10) ||
                ((phoneNum.StartsWith("10") || phoneNum.StartsWith("12") || phoneNum.StartsWith("13") ||
                phoneNum.StartsWith("14") || phoneNum.StartsWith("15") || phoneNum.StartsWith("16") ||
                phoneNum.StartsWith("17") || phoneNum.StartsWith("18") || phoneNum.StartsWith("19")) && phoneNum.Length == 9)) ||
                (phoneNum.StartsWith("0") && phoneNum.Length == 11 && phoneNum.Substring(1, 2) == "11") ||
                (phoneNum.StartsWith("0") && phoneNum.Length == 10 && phoneNum.Substring(1, 2) == "10") ||
                (phoneNum.StartsWith("0") && phoneNum.Length == 10 && (phoneNum.Substring(1, 2) == "12" || phoneNum.Substring(1, 2) == "13" || phoneNum.Substring(1, 2) == "14" ||
                                                                     phoneNum.Substring(1, 2) == "15" || phoneNum.Substring(1, 2) == "16" || phoneNum.Substring(1, 2) == "17" ||
                                                                     phoneNum.Substring(1, 2) == "18" || phoneNum.Substring(1, 2) == "19")))
            {
                
                return true;
            }
            else
            {
                Console.WriteLine(phoneNum);
                result = Json(new { success = false, message = "Invalid phone number format. (0123456789) or (123456789)" });
                return false;
            }
        }

        private bool IsValidDateCreated(string dateCreatedValue, out DateTime dateCreated, out JsonResult result)
        {
            dateCreated = default;  // Initialize the output parameter

            string[] expectedFormats = {
            "dd/MM/yyyy HH:mm", "d/M/yyyy H:m", "dd/MM/yyyy H:mm",
            "d/MM/yyyy HH:mm", "dd/M/yyyy HH:mm",
            "dd-MM-yyyy HH:mm", "d-MM-yyyy H:mm", "d-MM-yyyy HH:mm",
            "d/MM/yyyy h:mm tt", "dd/MM/yyyy h:mm tt",
            "dd/MM/yyyy hh:mm tt", "d/MM/yyyy hh:mm tt"
            };

            if (string.IsNullOrEmpty(dateCreatedValue) ||
            !DateTime.TryParseExact(dateCreatedValue, expectedFormats,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out dateCreated))
            {
                // Fallback: Try general parsing
                if (!DateTime.TryParse(dateCreatedValue, out dateCreated))
                {
                    result = Json(new
                    {
                        success = false,
                        message = "Invalid DateCreated format. Expected formats: dd/MM/yyyy HH:mm",
                    });
                    return false;
                }
            }

            DateTime today = DateTime.Now;
            DateTime hundredYearsAgo = today.AddYears(-100);

            if (dateCreated > today)
            {
                result = Json(new { success = false, message = "DateCreated cannot be in the future." });
                return false;
            }

            if (dateCreated < hundredYearsAgo)
            {
                result = Json(new { success = false, message = "Date Created cannot be more than 100 years in the past." });
                return false;
            }

            result = null;
            return true;
        }

        private bool IsValidDOB(string dobValue, out DateOnly dob, out JsonResult result)
        {
            dob = default;  // Initialize the output parameter

            if (!string.IsNullOrEmpty(dobValue) && dobValue.Contains(" "))
            {
                dobValue = dobValue.Split(' ')[0];  // Remove time component
            }

            if (!DateOnly.TryParseExact(dobValue, "dd/MM/yyyy", out dob) &&
                !DateOnly.TryParseExact(dobValue, "d/M/yyyy", out dob) &&
                !DateOnly.TryParseExact(dobValue, "dd/M/yyyy", out dob) &&
                !DateOnly.TryParseExact(dobValue, "d/MM/yyyy", out dob))
            {
                Console.WriteLine(dobValue);
                result = Json(new { success = false, message = "Invalid DOB format. Expected format: dd/MM/yyyy." });
                return false;
            }

            // Validate age range
            int currentYear = DateTime.Now.Year;
            int age = currentYear - dob.Year;

            if (dob > DateOnly.FromDateTime(DateTime.Now).AddYears(-age))
            {
                age--;
            }

            if (age < 18 || age > 100)
            {
                result = Json(new { success = false, message = "DOB must correspond to an age between 18 and 100 years." });
                return false;
            }

            result = null;
            return true;
        }
        //IMPORT AND EXPORT BASED ON EXCEL
        //UPDATE USING EXCEL





        //EDIT ADMIN OR MANAGER OWN PROFILE
        //SELF PROFILE AND PASSWORD
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult AdminProfile()
        {
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = GetUserByEmail(userEmail);

            if (user == null)
            {
                return NotFound(); // Return a 404 if user is not found
            }

            var userModel = GetUsersModels(user.Id);

            if (userModel == null)
            {
                return NotFound(); // Return a 404 if the profile is not found
            }

            return View(userModel); // Pass the user model to the view
        }

        private HotelRoomReservationSystem.Models.Users GetUserByEmail(string email)
        {
            return db.Users.FirstOrDefault(u => u.Email == email);
        }

        private UpdateUserVM GetUsersModels(string userId)
        {
            var user = db.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return null; // Return null if the user is not found
            }

            var nameParts = user.Name?.Split(' ') ?? Array.Empty<string>();
            var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
            var lastName = string.Join(" ", nameParts.Skip(1));

            var userDetails = new UpdateUserVM
            {
                id = user.Id,
                FirstName = firstName,
                LastName = lastName,
                Email = user.Email,
                PhoneNum = user.PhoneNum,
                portrait = user.Portrait,
                BirthDay = user.DOB
            };

            return userDetails;
        }

        [Authorize(Roles = "Admin,Manager")]
        public IActionResult editAdminPassword()
        {
            return View("editAdminPassword");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult editAdminPassword(ResetPasswordVM model)
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var email = claimsIdentity.FindFirst(ClaimTypes.Name)?.Value;
            var u = db.Users.FirstOrDefault(user => user.Email == email);

            if (model.currentPassword == null)
            {
                ModelState.AddModelError("currentPassword", "Current Password is Required.");
                return View(model);
            }

            if (!hp.VerifyPassword(model.currentPassword, u.Password))
            {
                ModelState.AddModelError("currentPassword", "Current Password Incorrect.");
                return View(model);
            }

            if (model.currentPassword.Equals(model.Password))
            {
                ModelState.AddModelError("Password", "Cuurent and New Password are Same.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                u.Password = hp.HashPassword(model.Password);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Password updated successfully!";

                return RedirectToAction("editAdminPassword");
            }
            else
            {
                //ModelState.AddModelError("", "Please Enter new Password accordingly.");
            }

            return View(model);

        }

        //EDIT ADMIN OR MANAGER OWN PROFILE
        //SELF PROFILE AND PASSWORD



        //UPDATE OWN PROFILE FOR ADMIN AND MANAGER

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult AdminProfile(UpdateUserVM model)
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var existingUser = db.Users.FirstOrDefault(u => u.Id == model.id);

            if (existingUser == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }

            if (!ValidateUserDetails(model, existingUser))
            {
                model.portrait = existingUser.Portrait;
                PrepareModelForReturn(model, existingUser);
                return View(model);
            }

            bool isUnchanged =
        existingUser.Name == $"{model.FirstName} {model.LastName}" &&
        existingUser.Email == model.Email &&
        existingUser.PhoneNum == model.PhoneNum &&
        existingUser.DOB == model.BirthDay &&
        string.IsNullOrEmpty(model.Base64Photo) &&
        model.Photo == null;

            if (isUnchanged)
            {
                TempData["infoMessage"] = "No changes detected in your profile.";
                return RedirectToAction("AdminProfile");
            }

            if (ModelState.IsValid)
            {
                UpdateExistingUser(existingUser, model);
                UpdateUserClaims(existingUser, claimsIdentity);

                db.SaveChanges();

                TempData["SuccessMessage"] = "Profile updated successfully!";
                
                return RedirectToAction("AdminProfile");
            }

            PrepareModelForReturn(model, existingUser);
            return View(model);
        }
        //UPDATE OWN PROFILE FOR ADMIN AND MANAGER

        //REUSABLE UPDATE CONCEPT
        private bool ValidateUserDetails(UpdateUserVM model, HotelRoomReservationSystem.Models.Users existingUser)
        {
            bool hasErrors = false;

            if (existingUser.Email != model.Email && hp.CheckEmailExist(model.Email))
            {
                ModelState.AddModelError("Email", "This Email has been registered. Please try another.");
                hasErrors = true;
            }

            if (existingUser.PhoneNum != model.PhoneNum && hp.CheckPhoneExist(model.PhoneNum))
            {
                ModelState.AddModelError("PhoneNum", "This Phone Number has been registered. Please try another.");
                hasErrors = true;
            }

            if (model.Photo != null)
            {
                string validationError = hp.ValidatePhoto(model.Photo);
                if (!string.IsNullOrEmpty(validationError))
                {
                    ModelState.AddModelError("Photo", validationError);
                    hasErrors = true;
                }
            }

            if (!string.IsNullOrEmpty(model.Base64Photo))
            {
                string validationError = hp.ValidatePhoto(null, model.Base64Photo);
                if (!string.IsNullOrEmpty(validationError))
                {
                    ModelState.AddModelError("Photo", validationError);
                    hasErrors = true;
                }
            }

            return !hasErrors;
        }

        private void UpdateExistingUser(HotelRoomReservationSystem.Models.Users existingUser, UpdateUserVM model)
        {
            existingUser.Name = model.FirstName + " " + model.LastName;
            existingUser.Email = model.Email;
            existingUser.PhoneNum = model.PhoneNum;
            existingUser.DOB = model.BirthDay;

            if (model.Photo != null || !string.IsNullOrEmpty(model.Base64Photo))
            {
                hp.DeletePhoto(existingUser.Portrait, "/images/user_photo/");
            }

            if (!string.IsNullOrEmpty(model.Base64Photo))
            {
                existingUser.Portrait = hp.SavePhoto(null, model.Base64Photo, "/images/user_photo/", existingUser.Id);
            }
            else if (model.Photo != null)
            {
                existingUser.Portrait = hp.SavePhoto(model.Photo, null, "/images/user_photo/", existingUser.Id);
            }
        }

        private void UpdateUserClaims(HotelRoomReservationSystem.Models.Users existingUser, ClaimsIdentity claimsIdentity)
        {
            var updatedClaims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, existingUser.Email),
        new Claim("Names", existingUser.Name),
        new Claim("Portrait", existingUser.Portrait),
    };

            claimsIdentity.RemoveClaim(claimsIdentity.FindFirst("Portrait"));
            claimsIdentity.RemoveClaim(claimsIdentity.FindFirst("Names"));
            claimsIdentity.AddClaims(updatedClaims);

            HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity));
        }

        private void PrepareModelForReturn(UpdateUserVM model, HotelRoomReservationSystem.Models.Users existingUser)
        {
            var nameParts = existingUser.Name.Split(' ');
            model.FirstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
            model.LastName = string.Join(" ", nameParts.Skip(1));
            model.Email = existingUser.Email;
            model.portrait = existingUser.Portrait;
            model.PhoneNum = existingUser.PhoneNum;
            model.BirthDay = existingUser.DOB;
        }

        //REUSABLE UPDATE CONCEPT



        //MESSAGE SECTION
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult contactAdminSide(string? searchBar, string? sort, string? dir, int page = 1, int pageSize = 5, string status = "All")
        {
            ViewBag.Name = searchBar = searchBar?.Trim() ?? "";
            ViewBag.PageSize = pageSize;
            ViewBag.Page = page;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;
            var messages = string.IsNullOrEmpty(searchBar)
            ? GetAllMessages()
            : GetMessagesById(searchBar);

            var messageDetails = messages.Select(message => new ContactMessagesVM
            {
                Id = message.Id,
                Name = message.Name,
                Email = message.Email,
                Phone = message.Phone,
                Messages = message.Messages,
                Status = message.Status,
                CreatedDate = message.CreatedDate,
                ResolvedDate = message.ResolvedDate,
                ReplyMessage = message.ReplyMessage,
            }).ToList();

            if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                messageDetails = messageDetails
                    .Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            dir = dir?.ToLower() == "des" ? "des" : "asc";


            Func<ContactMessagesVM, object> fn = sort switch
            {
                "MessageId" => r => r.Id,
                "Name" => r => r.Name,
                "Phone" => r => r.Phone,
                "Email" => r => r.Email,
                "Message" => r => r.Messages,
                "ReplyMessage" => r => r.Messages,
                "Status" => r => r.Status,
                "CreatedDate" => r => r.CreatedDate,

                "ResolvedDate" => r => r.ResolvedDate,
                _ => r => r.Id // Default sorting column
            };

            var sorted = dir == "des" ? messageDetails.OrderByDescending(fn) : messageDetails.OrderBy(fn);

            if (pageSize == 0)
            {
                pageSize = messages.Count();
            }


            if (page < 1) // Ensure page number is valid
            {
                return RedirectToAction(null, new { searchBar, sort, dir, page = 1, pageSize, status });
            }

            var m = sorted.ToPagedList(page, pageSize); // Set page size = 5

            if (page > m.PageCount && m.PageCount > 0) // Redirect if page exceeds total pages
            {
                return RedirectToAction(null, new { searchBar, sort, dir, page = m.PageCount, pageSize, status });
            }

            if (Request.IsAjax())
            {
                return PartialView("_ContactList", m); // Return only the updated reservation list and pagination controls
            }

            return View("contactAdminSide", m);
        }


        [Authorize(Roles = "Admin,Manager")]
        public List<HotelRoomReservationSystem.Models.Message> GetAllMessages()
        {
            return db.Message
                     .Where(m => m.Status != "DELETED") // Filter out messages with status "DELETED"
                     .ToList();
        }

        [Authorize(Roles = "Admin,Manager")]
        public List<HotelRoomReservationSystem.Models.Message> GetMessagesById(string searchBar)
        {
            return db.Message
                     .Where(m => m.Status != "DELETED" && m.Id.Contains(searchBar)) // Filter and search by Id
                     .ToList();
        }

        [Authorize(Roles = "Admin,Manager")]
        public HotelRoomReservationSystem.Models.Message GetMessageById(string messageId)
        {
            var message = db.Message.FirstOrDefault(u => u.Id == messageId && u.Status != "DELETED");
            if (message == null)
            {
                throw new InvalidOperationException($"Message with ID {messageId} not found or has been deleted.");
            }
            return message;
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public IActionResult MessageDelete([FromBody] List<string> messages)
        {
            if (messages == null)
            {
                return BadRequest(new { message = "No Message selected for deletion." });
            }

            try
            {
                RemoveMessages(messages);
                return Ok(new { message = $"{messages.Count} Messages deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while deleting Messages.", details = ex.Message });
            }
        }

        [Authorize(Roles = "Manager")]
        public void RemoveMessages(List<string> messages)
        {
            // Fetch all users whose IDs match the list
            var messagesToRemove = db.Message.Where(message => messages.Contains(message.Id)).ToList();

            if (messagesToRemove.Count == 0)
            {
                Console.WriteLine("No users found to remove.");
                return;
            }

            // Update the status for all fetched users
            foreach (var msg in messagesToRemove)
            {
                msg.Status = "DELETED";
            }

            db.SaveChanges();
            Console.WriteLine($"{messagesToRemove.Count} users marked as 'Deleted'.");
        }


        //SUBMIT REPLY
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult SubmitReply(string messageId, string replyMessage)
        {
            if (string.IsNullOrWhiteSpace(replyMessage))
            {
                return Json(new { success = false, message = "Reply message cannot be empty." });
            }
            if (replyMessage.Length > 180)
            {
                return Json(new { success = false, message = "Reply message cannot exceed 180 characters." });
            }
            Console.WriteLine(messageId);
            Console.WriteLine(replyMessage);
            var message = db.Message.FirstOrDefault(m => m.Id == messageId);
            if (message == null)
            {
                return Json(new { success = false, message = "Message not found." });
            }

            // Save the reply (example logic)
            message.ReplyMessage = replyMessage;
            message.ResolvedDate = DateTime.Now;
            message.Status = "CLOSE";
            db.SaveChanges();
            ReplyEmailToGuest(messageId,replyMessage);

            return Json(new { success = true, message = "Reply submitted successfully." });
        }

        public Task ReplyEmailToGuest(string msgId, string msg)
        {
            var msgs = db.Message
                .Where(m => m.Id == msgId)
                .FirstOrDefault();

            if (msgs != null)
            {
                StringBuilder mailBody = new StringBuilder();
                mailBody.Append("<html><head></head><body>");
                mailBody.Append("<p>Dear User,</p>");
                mailBody.Append($"<p>Question :{msgs.Messages}</p>");
                mailBody.Append($"<p>Response: {msgs.ReplyMessage}</p>");
                mailBody.Append($"<p>Reply Date : {msgs.ResolvedDate}</p>");
                mailBody.Append("<p>Regards,</p>");
                mailBody.Append("<p>Hotel Team</p>");
                mailBody.Append("</body></html>");
                bool emailSent = SendEmail("yeapzijia1936@gmail.com", msgs.Email, $"Contact Response For {msgs.Email}", mailBody.ToString());

                if (emailSent)
                {
                    return Task.CompletedTask;
                }
                else
                {
                    
                }
            }
            else
            {
                
            }
            return Task.CompletedTask;
        }

























        //[HttpPost]
        //[Authorize(Roles = "Admin,Manager")]
        //public async Task<IActionResult> UploadUserExcel(IFormFile file)
        //{
        //    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        //    if (file != null && file.Length > 0)
        //    {
        //        var uploadFolder = $"{Directory.GetCurrentDirectory()}\\wwwroot\\file\\";

        //        if (!Directory.Exists(uploadFolder))
        //        {
        //            Directory.CreateDirectory(uploadFolder);
        //        }

        //        var filePath = Path.Combine(uploadFolder, file.FileName);

        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            await file.CopyToAsync(stream);
        //        }
        //        using (var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read))
        //        {
        //            // Auto-detect format, supports:
        //            //  - Binary Excel files (2.0-2003 format; *.xls)
        //            //  - OpenXml Excel files (2007 format; *.xlsx, *.xlsb)
        //            using (var reader = ExcelReaderFactory.CreateReader(stream))
        //            {
        //                do
        //                {
        //                    bool isHeaderSkipped = false;

        //                    while (reader.Read())
        //                    {
        //                        if (!isHeaderSkipped)
        //                        {
        //                            isHeaderSkipped = true;
        //                            continue;
        //                        }

        //                        string role = reader.GetValue(9).ToString();
        //                        HotelRoomReservationSystem.Models.Users users = role switch
        //                        {
        //                            "Manager" => new HotelRoomReservationSystem.Models.Manager(),
        //                            "Admin" => new HotelRoomReservationSystem.Models.Admin(),
        //                            "Customer" => new HotelRoomReservationSystem.Models.Customer(),
        //                            _ => new HotelRoomReservationSystem.Models.Users(), // Fallback to base class
        //                        };
        //                        users.Id = reader.GetValue(0).ToString();
        //                        users.Name = reader.GetValue(1).ToString();
        //                        users.Email = reader.GetValue(2).ToString();
        //                        users.Password = reader.GetValue(3).ToString();
        //                        users.PhoneNum = reader.GetValue(4).ToString();
        //                        users.DateCreated = DateTime.Parse(reader.GetValue(5).ToString());
        //                        users.DOB = DateOnly.Parse(reader.GetValue(6).ToString());
        //                        users.Portrait = reader.GetValue(7).ToString();
        //                        users.Status = reader.GetValue(8).ToString();

        //                        var existingUser = db.Users.FirstOrDefault(u => u.Id == users.Id);
        //                        if (existingUser != null)
        //                        {
        //                            return Json(new { success = false, message = $"User with ID {users.Id} already exists." });
        //                        }

        //                        db.Add(users);


        //                        await db.SaveChangesAsync();
        //                    }
        //                } while (reader.NextResult());

        //            }
        //        }
        //        Console.WriteLine("Excel file processed and users added to the database successfully!");
        //        return Json(new { success = true, message = "Excel file processed and users added to the database successfully!" });
        //    }
        //    else
        //    {
        //        return Json(new { success = false, message = "No file uploaded or the file is empty." });
        //    }

        //}
    }
}
