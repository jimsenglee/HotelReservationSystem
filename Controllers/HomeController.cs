//using HotelRoomReservationSystem.Migrations;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Security.Claims;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using HotelRoomReservationSystem.BLL;
using HotelRoomReservationSystem.Models;
using HotelRoomReservationSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace HotelRoomReservationSystem.Controllers
{
    public class HomeController : Controller
    {

        private readonly HotelRoomReservationDB db;
        private readonly Helper hp;
        public HomeController(HotelRoomReservationDB db, Helper hp)
        {
            this.db = db;
            this.hp = hp;
        }

        public IActionResult Index()
        {
            hp.CheckWaitingList();
            var topSalesWithImages = db.RoomType
       .Select(rt => new RTWithImgVM
       {
           RtId = rt.Id,
           RtName = rt.Name,
           TotalPrice = db.Transaction
               .Where(t => db.Reservation
                   .Where(r => r.Room.RoomTypeId == rt.Id)
                   .Select(r => r.Id)
                   .Contains(t.ReservationId) && t.Status == "Completed")
               .Sum(t => (decimal?)t.Amount) ?? 0,
           Price = rt.Price,
           Images = db.RoomTypeImages
               .Where(img => img.RoomTypeId == rt.Id)
               .Select(img => img.Name) // Assuming ImageUrl is the property for the image path
               .FirstOrDefault()
       })
       .OrderByDescending(x => x.Price)
       .Take(3)
       .ToList();



            var feedbackList = db.Feedback
                         .Where(f => f.Description != null)
                         .GroupBy(f => f.UserId)
                         .Select(g => g.FirstOrDefault())
                         .Take(6)
                         .ToList();

            var userIds = feedbackList.Select(f => f.UserId).Distinct().ToList();
            var userList = db.Users.Where(u => userIds.Contains(u.Id)).ToList();
            var viewModel = new HomePageVM
            {
                FeedbackList = feedbackList,
                UserList = userList,
                TypeList = topSalesWithImages,
            };

            if (User.Identity.IsAuthenticated)
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "Manager" || userRole == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return View(viewModel);
                }
            }

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult storeTempData(string roomId)
        {
            // Store the selected roomId in TempData
            TempData["SelectedRoomId"] = roomId;
            // Return a simple JSON response indicating success
            return Json(new { success = true });
        }
        //[HttpPost]
        //public IActionResult RoomDetails(string id)
        //{
        //    if (string.IsNullOrEmpty(id))
        //    {
        //        return RedirectToAction("Rooms"); // Redirect back if no ID is provided
        //    }

        //    var room = db.Rooms.FirstOrDefault(r => r.Id == id);

        //    if (room == null)
        //    {
        //        return NotFound(); // Handle the case where the room doesn't exist
        //    }

        //    return View("RoomDetails", room);
        //    //var roomDetails = GetRoomDetailsById(roomId); // Replace with your data-fetching logic

        //    //// Pass the data to the "RoomDetails" view
        //    //return View(roomDetails);
        //    //var room = _roomService.GetRoomDetails(roomId);
        //    //return View(roomId);
        //}

        //private Room GetRoomDetailsById(string id)
        //{
        //    // Replace this with your database fetching logic
        //    return new Room { Id = id, Name = "Sample Room", Description = "A great room!" };
        //}
        //[HttpPost]
        //public JsonResult StoreRoomIdAndRedirect([FromBody] Rooms model)
        //{
        //    if (string.IsNullOrEmpty(model.Id))
        //    {
        //        return Json(new { success = false });
        //    }

        //    TempData["SelectedRoomId"] = model.Id;

        //    return Json(new
        //    {
        //        success = true,
        //        redirectUrl = Url.Action("RoomDetails", "Rooms")
        //    });
        //}

        //[HttpPost]
        //public IActionResult RoomDetails([FromForm] string roomId)
        //{
        //    // Debug logging to check the value of roomId
        //    if (string.IsNullOrEmpty(roomId))
        //    {
        //        Console.WriteLine("Room ID not received.");
        //        return BadRequest("Room ID is required.");
        //    }

        //    // Retrieve the room from the database
        //    var room = db.Rooms.FirstOrDefault(r => r.Id == roomId);

        //    if (room == null)
        //    {
        //        return NotFound($"Room with ID {roomId} not found.");
        //    }

        //    Console.WriteLine("FOunded");

        //    // Render the RoomDetails view with the room data
        //    return View("RoomDetails", room);
        //}


        public IActionResult RoomDetails()
        {
            // Retrieve the roomId from TempData
            var roomId = TempData["SelectedRoomId"]?.ToString();

            if (string.IsNullOrEmpty(roomId))
            {
                // Handle case when roomId is not found or TempData is empty
                return RedirectToAction("Rooms");
            }

            // Find the room by the selected roomId
            var room = db.Rooms.FirstOrDefault(r => r.Id == roomId);
            if (room == null)
            {
                return RedirectToAction("Rooms");  // If room not found, go back to rooms
            }
            db.Entry(room).Reference(r => r.RoomType).Load();

            return View("RoomDetails", room);  // Return the RoomDetails view with the room data
        }


        [Route("AboutUs")]
        public async Task<IActionResult> AboutUs()
        {
            var manager = db.Users
                .AsEnumerable()
            .Where(u => u.Role == "Manager")
            .FirstOrDefault();

            var admins = db.Users
                .AsEnumerable()
            .Where(u => u.Role == "Admin")
            .Take(2)
            .ToList();

            //var filteredUsers = new List<HotelRoomReservationSystem.Models.Users>();
            //if (manager != null) filteredUsers.Add(manager);
            //filteredUsers.AddRange(admins);
            var userData = new
            {
                Manager = manager,
                Admins = admins
            };

            return View(userData);
        }

        [Route("Contact")]
        public IActionResult Contact()
        {
            return View();
        }


        [HttpPost]
        [Route("/Home/ContactForm")]
        public async Task<IActionResult> Contact([FromBody] ContactMessageVM model)
        {
            Console.WriteLine("IN");
            Console.WriteLine(model.Name);
            Console.WriteLine(model.Message);
            Console.WriteLine(model.Email);
            Console.WriteLine(model.Phone);
            if (ModelState.IsValid)
            {
                Console.WriteLine("ININ");
                string msgId = await GenerateMsgIdAsync();
                var contactMessage = new Message
                {
                    Id = msgId,
                    Name = model.Name,
                    Phone = model.Phone,
                    Email = model.Email,
                    Messages = model.Message,
                    ReplyMessage = "-",
                    Status = "OPEN",
                    CreatedDate = DateTime.UtcNow
                };

                db.Message.Add(contactMessage);
                db.SaveChanges();
                return Json(new
                {
                    success = true,
                    message = "Your message has been submitted successfully! Please wait 3 working days for a reply."
                });
            }
            Console.WriteLine("OUT");
            var errors = ModelState
        .Where(m => m.Value.Errors.Any())
        .ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Errors
                .Select(e => GetCustomErrorMessage(e))
                .ToArray()
        );
            foreach (var error in errors)
            {
                foreach (var message in error.Value)
                {
                    Console.WriteLine($"Field: {error.Key}, Error: {message}");
                }
            }
            return Json(new
            {
                success = false,
                message = "Validation errors occurred.",
                errors = errors,
                model = model // Include the submitted data
            });

        }

        private async Task<string> GenerateMsgIdAsync()
        {

            string newUMsgId;

            {
                do
                {
                    string currentYear = DateTime.Now.Year.ToString().Substring(2);
                    var lastUser = db.Message
                    .Where(u => u.Id.StartsWith($"M{currentYear}"))
                    .OrderByDescending(u => u.Id)
                    .FirstOrDefault();

                    if (lastUser == null)
                    {
                        newUMsgId = $"M{currentYear}000001";
                    }
                    else
                    {
                        string numericPart = lastUser.Id.Substring(5);  // Get the last 6 digits (after 'U{year}')
                        int numericValue = int.Parse(numericPart);
                        numericValue++;

                        newUMsgId = $"M{currentYear}{numericValue.ToString("D6")}";
                    }

                    bool idExists = db.Message.Any(u => u.Id == newUMsgId);

                    if (!idExists)
                    {
                        break;  // ID is unique, exit loop
                    }
                } while (true);
            }

            return newUMsgId;
        }

        private string GetCustomErrorMessage(ModelError error)
        {
            // Customize error messages here
            switch (error.ErrorMessage)
            {
                case "The Name field is required.":
                    return "Please provide your full name.";
                case "Invalid phone number format.":
                    return "Please provide correct phone number format(0123456789)";
                case "The Phone field is required.":
                    return "Your phone number is required.";
                case "The Email field is required.":
                    return "Please provide your email address.";
                case "Invalid Email Address Format.":
                    return "Please provide a valid email address.";
                case "The Message field is required.":
                    return "Please write your message.";
                case "Name length should be within 20 character.":
                    return "Name length should be within 20 character.";
                case "Message length should be within 200 character.":
                    return "Message length should be within 200 character.";
                default:
                    return error.ErrorMessage;
            }
        }

        public IActionResult TermAndCondition()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult EnvironmentPolicy()
        {
            return View();
        }


        //[HttpPost]
        //public async Task<IActionResult> getAvailableRoom(searchAVM model)
        //{
        //    var searchModel = model.SearchModel;
        //    var roomTypes = model.RoomTypes;
        //    //if (!ModelState.IsValid)
        //    //{
        //    //    return RedirectToAction("Rooms", "RoomType");
        //    //}
        //    Console.WriteLine(searchModel.children);
        //    Console.WriteLine(searchModel.adults);
        //    Console.WriteLine("model.checkinDate");
        //    Console.WriteLine(searchModel.checkinDate);
        //    Console.WriteLine(searchModel.checkoutDate);
        //    if (model == null)
        //    {
        //        Console.WriteLine("NULL");
        //    }


        //    if (ModelState.IsValid)
        //    {
        //        var format = "d MMMM, yyyy";
        //        var culture = CultureInfo.InvariantCulture;

        //        DateOnly checkinDate;
        //        DateOnly checkoutDate;

        //        if (DateTime.TryParseExact(searchModel.checkinDate, format, culture, DateTimeStyles.None, out DateTime parsedCheckinDate) &&
        //DateTime.TryParseExact(searchModel.checkoutDate, format, culture, DateTimeStyles.None, out DateTime parsedCheckoutDate))
        //        {
        //            Console.WriteLine("HALLO");
        //            checkinDate = new DateOnly(parsedCheckinDate.Year, parsedCheckinDate.Month, parsedCheckinDate.Day);
        //            checkoutDate = new DateOnly(parsedCheckoutDate.Year, parsedCheckoutDate.Month, parsedCheckoutDate.Day);
        //            int adults = int.Parse(searchModel.adults);
        //            int children = int.Parse(searchModel.children);

        //            var availableRooms = checkAvailability(checkinDate, checkoutDate, adults, children);
        //            var searchModels = new searchAVM
        //            {
        //                SearchModel = new FindAvailableRoomTypeVM
        //                {
        //                    checkinDate = checkinDate.ToString("yyyy-MM-dd"),
        //                    checkoutDate = checkoutDate.ToString("yyyy-MM-dd"),
        //                    adults = adults.ToString(),
        //                    children = children.ToString()
        //                },
        //                RoomTypes = availableRooms
        //            };

        //            if (searchModels.RoomTypes != null && searchModels.RoomTypes.Any())
        //            {
        //                foreach (var roomType in searchModels.RoomTypes)
        //                {
        //                    // Display RoomType details
        //                    Console.WriteLine($"Room Type: {roomType.Name}, Price: {roomType.Price}");
        //                }
        //            }
        //            return RedirectToAction("Roomss", "RoomType", new { searchModels });

        //        }

        //    }

        //    else
        //    {
        //        Console.WriteLine("NOT HERE");
        //        Console.WriteLine("ModelState is not valid.");
        //        foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
        //        {
        //            Console.WriteLine(error.ErrorMessage);
        //        }
        //        return RedirectToAction("Rooms", "RoomType");
        //    }
        //    return RedirectToAction("Rooms", "RoomType");
        //}

        //public List<RoomType> checkAvailability(DateOnly checkinDate,DateOnly checkoutDate,int children,int adult)
        //{
        //    var Reservation = db.Reservation.ToList();
        //    DateTime checkinDateTime = new DateTime(checkinDate.Year, checkinDate.Month, checkinDate.Day);
        //    DateTime checkoutDateTime = new DateTime(checkoutDate.Year, checkoutDate.Month, checkoutDate.Day);
        //    //var occupiedRooms = Reservation.Where(rs => rs.CheckOutDate >= checkinDateTime && rs.CheckInDate < checkoutDateTime)
        //    //                        .Select(rs => rs.RoomId) 
        //    //                        .ToList();
        //    //var availableRooms = db.RoomType
        //    //              .Where(rt => !occupiedRooms.Contains(rt.Id))
        //    //              .ToList();
        //    var reservations = db.Reservation.ToList();
        //    var roomTypes = db.RoomType.ToList();
        //    var occupiedRoomIds = reservations
        // .Where(rs => rs.CheckOutDate >= checkinDateTime && rs.CheckInDate < checkoutDateTime)
        // .Select(rs => rs.RoomId) // Corrected to use RoomId
        // .ToList();



        //    var availableRooms = roomTypes
        //.Where(rt => rt.Capacity >= (children + adult)) // Ensure capacity meets needs
        //.Select(rt => new
        //{
        //    RoomType = rt,
        //    AvailableQuantity = rt.Quantity - occupiedRoomIds.Count(id => id == rt.Id) // Use RoomId here
        //})
        //.Where(rt => rt.AvailableQuantity > 0) // Ensure there are available rooms
        //.Select(rt => rt.RoomType) // Extract the RoomType from the anonymous type
        //.ToList();
        //    var roomTypeIds = availableRooms.Select(rt => rt.Id).ToList(); // Extracting only the RoomType Ids
        //    Console.WriteLine(roomTypeIds);
        //    return availableRooms;

        //}


    }
}
