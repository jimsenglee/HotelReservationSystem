using Microsoft.AspNetCore.Mvc;
using HotelRoomReservationSystem.Models.ViewModels;
using HotelRoomReservationSystem.Models;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using HotelRoomReservationSystem.Models;
using Microsoft.AspNetCore.Http;
using PayPal.Api;
using System.Configuration;
using Transaction = PayPal.Api.Transaction;
using System;
using Newtonsoft.Json;
using Stripe.Checkout;
using Stripe;
using HotelRoomReservationSystem.BLL;
using System.Net.Mail;
using System.Security.Claims;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using ClosedXML.Excel;
using System.Data;
using System.Text.Json;
using HotelRoomReservationSystem.BLL.Interfaces;

namespace HotelRoomReservationSystem.Controllers
{
    public class ReservationController : Controller
    {
        private readonly HotelRoomReservationDB db;
        private readonly IWebHostEnvironment en;
        private readonly IConfiguration _configuration;
        private readonly Helper hp;
        private readonly IMembershipService membershipService;
        private readonly IRewardsService rewardsService;
        private readonly IMembershipRewardsService membershipRewardsService;
        public ReservationController(HotelRoomReservationDB context, IConfiguration configuration, IWebHostEnvironment en, Helper hp, IMembershipService membershipService,
           IRewardsService rewardsService,
           IMembershipRewardsService membershipRewardsService)
        {
            db = context;
            _configuration = configuration;
            this.en = en;
            this.hp = hp;

            this.membershipService = membershipService ?? throw new ArgumentNullException(nameof(membershipService));
            this.rewardsService = rewardsService ?? throw new ArgumentNullException(nameof(rewardsService));
            this.membershipRewardsService = membershipRewardsService ?? throw new ArgumentNullException(nameof(membershipRewardsService));
        }


        //private DateTime GetCurrentDate()
        //{
        //    // Use this line to set a fixed date for testing
        //    //return new DateTime(2024, 12, 26); // Change this to the date you want to simulate

        //    // Uncomment this line in production to use the actual server date
        //    return DateTime.Now;
        //}


        private void AutoCancelExpiredReservations()
        {
            var expiredReservations = db.Reservation
                .Include(r => r.Room)
                .Where(r => r.Status == "Pending" && r.CheckInDate < DateTime.Today) // Use DateTime.Today for consistent date comparison
                .ToList();

            if (expiredReservations.Any())
            {
                foreach (var reservation in expiredReservations)
                {
                    System.Diagnostics.Debug.WriteLine($"AutoCancelExpiredReservations: Cancelling Reservation ID = {reservation.Id}, Room ID = {reservation.RoomId}");

                    // Cancel the reservation
                    reservation.Status = "Canceled";

                    // Cancel the associated transaction if it exists
                    var transaction = db.Transaction.FirstOrDefault(t => t.ReservationId == reservation.Id && t.Status == "Pending");
                    if (transaction != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"AutoCancelExpiredReservations: Cancelling Transaction ID = {transaction.Id}");
                        transaction.Status = "Canceled";
                        db.Transaction.Update(transaction);
                    }
                }

                // Save changes to the database
                db.SaveChanges();
            }
        }

        //// below is old one pelase dont delete it is for RoomTypeDetails
        [HttpGet]
        public IActionResult CheckRoomTypeAvailability(string roomTypeId, DateTime checkInDate, DateTime checkOutDate)
        {
            if (string.IsNullOrEmpty(roomTypeId))
            {
                return Json(new { isAvailable = false, message = "Room Type is required." });
            }

            if (checkInDate >= checkOutDate)
            {
                return Json(new { isAvailable = false, message = "Check-Out date must be later than Check-In date." });
            }

            bool isAvailable = IsRoomAvailable(roomTypeId, checkInDate, checkOutDate);

            if (!isAvailable)
            {
                var alternativeDates = GetAlternativeDates(roomTypeId, checkInDate, checkOutDate);
                return Json(new
                {
                    isAvailable = false,
                    message = "No rooms available for the selected dates. Please try another date or room type.",
                    alternatives = alternativeDates
                });
            }

            return Json(new { isAvailable = true, message = "Room is available!" });
        }


        [HttpGet]
        public IActionResult CheckRoomAvailability(string roomTypeId, DateTime checkInDate, DateTime checkOutDate)
        {
            if (string.IsNullOrEmpty(roomTypeId))
            {
                return Json("Room Type is required.");
            }

            if (checkInDate >= checkOutDate)
            {
                return Json("Check-Out date must be later than Check-In date.");
            }

            bool isAvailable = IsRoomAvailable(roomTypeId, checkInDate, checkOutDate);

            if (!isAvailable)
            {
                return Json("The selected room type is not available for the given dates.");
            }

            return Json(true);
        }

        public bool IsRoomAvailable(string roomTypeId, DateTime checkInDate, DateTime checkOutDate)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Checking availability for RoomTypeId: {roomTypeId}, CheckInDate: {checkInDate}, CheckOutDate: {checkOutDate}");

            var occupiedRooms = db.Reservation
                .Where(res =>
                    res.Room.RoomTypeId == roomTypeId &&
                    (res.Status == "Pending" || res.Status == "Completed") &&
                    res.CheckInDate < checkOutDate &&
                    res.CheckOutDate > checkInDate) // Overlapping condition
                .Select(res => res.RoomId)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Occupied Rooms: {string.Join(", ", occupiedRooms)}");

            bool isRoomAvailable = db.Rooms.Any(r => r.RoomTypeId == roomTypeId && !occupiedRooms.Contains(r.Id));
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Is Room Available: {isRoomAvailable}");

            return isRoomAvailable;
        }

        private List<object> GetAlternativeDates(string roomTypeId, DateTime checkInDate, DateTime checkOutDate)
        {
            int requestedDays = (checkOutDate - checkInDate).Days;
            var allReservations = db.Reservation
                .Include(r => r.Room)
                .Where(res => res.Room.RoomTypeId == roomTypeId && res.Status != "Canceled")
                .OrderBy(res => res.CheckInDate)
                .ToList();

            List<object> suggestedDates = new List<object>();

            DateTime start = DateTime.Today > checkInDate ? DateTime.Today : checkInDate;
            DateTime end = DateTime.Today.AddDays(90);

            allReservations.Add(new Reservation
            {
                CheckInDate = end,
                CheckOutDate = end
            });

            foreach (var reservation in allReservations)
            {
                // see if there is enough gap between start and the current reservation
                if ((reservation.CheckInDate - start).Days >= requestedDays)
                {
                    suggestedDates.Add(new
                    {
                        startDate = start.ToString("yyyy-MM-dd"),
                        endDate = start.AddDays(requestedDays).ToString("yyyy-MM-dd")
                    });
                }

                // change start to the end of the current reservation
                start = reservation.CheckOutDate;
            }

            return suggestedDates;
        }

        public bool ValidateCheckInDate(DateTime checkInDate)
        {
            return checkInDate >= DateTime.Today;
        }

        public bool ValidateCheckOutDate(DateTime checkInDate, DateTime checkOutDate)
        {
            return checkOutDate > checkInDate;
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateReservation(CreateReservationViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            AutoCancelExpiredReservations();

            // retrieve logged-in user information
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = db.Users.FirstOrDefault(u => u.Email == userEmail);
            // determine role-based user info
            if (user.Role == "Manager") // Admin Flow
            {
                model.UsersId = null;

                // see for UserName and UserEmail manually for admin flow
                if (string.IsNullOrEmpty(model.UserName))
                {
                    ModelState.AddModelError("UserName", "Customer Name is required.");
                }
                if (string.IsNullOrEmpty(model.UserEmail))
                {
                    ModelState.AddModelError("UserEmail", "Email is required.");
                }
            }
            else if (user.Role == "Customer")
            {
                model.UsersId = user.Id;
                model.UserName = user.Name;
                model.UserEmail = user.Email;
            }



            var roomType = db.RoomType.FirstOrDefault(rt => rt.Id == model.RoomTypeId);
            if (roomType == null)
            {
                ModelState.AddModelError("RoomTypeId", "Selected room type not found.");
            }

            if (!ValidateCheckInDate(model.CheckInDate))
            {
                ModelState.AddModelError("CheckInDate", "Check-in date cannot be in the past.");
            }

            if (!ValidateCheckOutDate(model.CheckInDate, model.CheckOutDate))
            {
                ModelState.AddModelError("CheckOutDate", "Check-out date must be later than check-in date.");
            }

            //  Check-In date is within 90 days
            if (model.CheckInDate < DateTime.Today || model.CheckInDate > DateTime.Today.AddDays(90))
            {
                ModelState.AddModelError("CheckInDate", $"Check-In date must be within the next 90 days ({DateTime.Today:yyyy-MM-dd} to {DateTime.Today.AddDays(90):yyyy-MM-dd}).");
            }

            //  Check-Out date is within 10 days after Check-In
            if (model.CheckOutDate < model.CheckInDate.AddDays(1) || model.CheckOutDate > model.CheckInDate.AddDays(10))
            {
                ModelState.AddModelError("CheckOutDate", $"Check-Out date must be within 10 days after Check-In ({model.CheckInDate.AddDays(1):yyyy-MM-dd} to {model.CheckInDate.AddDays(10):yyyy-MM-dd}).");
            }

            // no same-day bookings
            if (model.CheckInDate == model.CheckOutDate)
            {
                ModelState.AddModelError("CheckOutDate", "Check-Out date cannot be the same as Check-In date.");
            }

            //  number of nights
            var nights = (model.CheckOutDate - model.CheckInDate).Days;
            if (nights <= 0)
            {
                ModelState.AddModelError("CheckOutDate", "Invalid stay duration.");
            }

            if (!ModelState.IsValid)
            {
                Debug.WriteLine("ModelState is invalid. Validation errors:");
                foreach (var error in ModelState)
                {
                    Debug.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }

                if (model.UsersId == null) // Admin flow
                {
                    Debug.WriteLine("Redirecting to CreateInfoForm for Admin.");
                    var roomTypes = db.RoomType.ToList();
                    model.RoomTypeList = roomTypes;

                    return View("CreateInfoForm", model); // Redirect to admin's form
                }
                else // Member flow
                {
                    Debug.WriteLine($"Redirecting to RoomTypeDetails for Member. RoomTypeId: {model.RoomTypeId}");
                    return RedirectToAction("RoomTypeDetails", "RoomType", new { id = model.RoomTypeId });
                }
            }



            // take all occupied rooms for the selected RoomType within the date range
            var occupiedRooms = db.Reservation
                .Where(res => res.Room.RoomTypeId == model.RoomTypeId &&
                            (res.Status == "Pending" || res.Status == "Completed") && // Include both Pending and Completed statuses
                              res.CheckInDate < model.CheckOutDate &&
                              res.CheckOutDate > model.CheckInDate)
                .Select(res => res.RoomId)
                .ToList();


            // take all rooms in the waiting list for the selected RoomType
            var waitingListRooms = db.WaitingList
                .Where(wl => wl.OrgRoomTypeId == model.RoomTypeId)
                .Select(wl => wl.RoomId)
                .ToList();


            // see the first available room
            var availableRoom = db.Rooms
                .Where(r => r.RoomTypeId == model.RoomTypeId &&
                            !occupiedRooms.Contains(r.Id) &&
                            !waitingListRooms.Contains(r.Id)) // Exclude rooms in the waiting list
                .FirstOrDefault();

            if (availableRoom == null)
            {
                ModelState.AddModelError("RoomTypeId", "No available rooms for the selected room type and date range.");
            }

            // total price
            decimal basePrice = roomType.Price * nights;
            decimal sst = basePrice * 0.06m; // 6% tax
            decimal guestFee = model.UsersId == null ? 10m : 0m; // RM10 guest fee for non-members
            model.TotalPrice = basePrice + sst + guestFee;

            string newId = GenerateNextReservationId();

            // (Admin vs Member)
            if (user.Role == "Manager")
            {
                var reservation = new Reservation
                {
                    Id = newId,
                    RoomId = availableRoom.Id,
                    UserName = model.UserName,
                    UserEmail = model.UserEmail,
                    CheckInDate = model.CheckInDate,
                    Status = "Pending",
                    CheckOutDate = model.CheckOutDate,
                    TotalPrice = model.TotalPrice,
                    DateCreated = DateTime.Now,
                    UsersId = null
                };

                db.Reservation.Add(reservation);
                db.SaveChanges();


                return RedirectToAction("ReserveList");
            }
            else
            {
                // store it in session for the checkout process
                var reservation = new Reservation
                {
                    RoomId = availableRoom.Id,
                    UserName = model.UserName,
                    UserEmail = model.UserEmail,
                    CheckInDate = model.CheckInDate,
                    Status = "Pending",
                    CheckOutDate = model.CheckOutDate,
                    TotalPrice = model.TotalPrice,
                    DateCreated = DateTime.Now,
                    UsersId = model.UsersId
                };

                //  reservation in session for checkout
                HttpContext.Session.Set("ReservationDetails", reservation);
                HttpContext.Session.Set("SelectedCapacity", model.SelectedCapacity);

                // Redirect to checkout page 
                return RedirectToAction("Checkout");
            }
        }

        [HttpPost]
        public IActionResult SetReservationForUpdate(string reservationId)
        {
            HttpContext.Session.SetString("ReservationId", reservationId);

            System.Diagnostics.Debug.WriteLine($"SetReservationForUpdate: Stored Reservation ID = {reservationId}");

            return RedirectToAction("UpdateReservation");
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpGet]
        public IActionResult UpdateReservation()
        {
            // Retrieve the reservation ID from the session
            var reservationId = HttpContext.Session.GetString("ReservationId");

            if (string.IsNullOrEmpty(reservationId))
            {
                System.Diagnostics.Debug.WriteLine("UpdateReservation: No Reservation ID found in session.");
                return RedirectToAction("ReserveList");
            }

            // Log for debugging purposes
            System.Diagnostics.Debug.WriteLine($"UpdateReservation: Retrieved Reservation ID = {reservationId}");

            // Fetch the reservation details
            var reservation = db.Reservation
                .Include(r => r.Room)
                .ThenInclude(room => room.RoomType)
                .FirstOrDefault(r => r.Id == reservationId);

            if (reservation == null)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateReservation: Reservation not found for ID = {reservationId}");
                return NotFound();
            }

            // Map the reservation details to CreateReservationViewModel
            var viewModel = new CreateReservationViewModel
            {
                RoomTypeId = reservation.Room.RoomTypeId,
                RoomTypeList = db.RoomType.ToList(), // Ensure RoomTypeList is populated
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                UserName = reservation.UserName,
                UserEmail = reservation.UserEmail,
                UsersId = reservation.UsersId,
                TotalPrice = reservation.TotalPrice
            };

            // Log the prefilled data for debugging
            System.Diagnostics.Debug.WriteLine($"UpdateReservation: Prefilled ViewModel = RoomTypeId = {viewModel.RoomTypeId}, UserName = {viewModel.UserName}, CheckInDate = {viewModel.CheckInDate}, CheckOutDate = {viewModel.CheckOutDate}");

            // Pass the view model to the view
            return View("UpdateReservation", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateReservationDetails(CreateReservationViewModel model)
        {
            // Validate reservation ID
            var reservationId = HttpContext.Session.GetString("ReservationId");
            if (string.IsNullOrEmpty(reservationId))
            {
                ModelState.AddModelError("", "Invalid reservation.");
                return View("UpdateReservation", model);
            }

            var reservation = db.Reservation
                .Include(r => r.Room)
                .FirstOrDefault(r => r.Id == reservationId);

            if (reservation == null)
            {
                ModelState.AddModelError("", "Reservation not found.");
                return View("UpdateReservation", model);
            }

            if (!ValidateCheckInDate(model.CheckInDate))
                ModelState.AddModelError("CheckInDate", "Check-in date cannot be in the past.");

            if (!ValidateCheckOutDate(model.CheckInDate, model.CheckOutDate))
                ModelState.AddModelError("CheckOutDate", "Check-out date must be later than check-in date.");

            if (ModelState.IsValid)
            {
                reservation.CheckInDate = model.CheckInDate;
                reservation.CheckOutDate = model.CheckOutDate;
                reservation.RoomId = db.Rooms.FirstOrDefault(r => r.RoomTypeId == model.RoomTypeId)?.Id;
                reservation.TotalPrice = model.TotalPrice;

                db.Reservation.Update(reservation);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Record has been successfully updated.";
                return RedirectToAction("ReserveList");
            }

            return View("UpdateReservation", model);
        }


        [Authorize(Roles = "Admin,Manager")]
        public IActionResult CreateInfoForm()
        {
            var roomTypes = db.RoomType
                .OrderBy(rt => rt.Name)
                .ToList();

            var roomTypeWithAvailability = roomTypes.Select(rt =>
            {
                var totalRooms = db.Rooms.Where(r => r.RoomTypeId == rt.Id).ToList();
                var occupiedRoomIds = db.Reservation
                    .Where(res => res.Room.RoomTypeId == rt.Id && res.Status == "Pending")
                    .Select(res => res.RoomId)
                    .ToList();

                var availableRooms = totalRooms.Where(r => !occupiedRoomIds.Contains(r.Id)).ToList();

                return new RoomType
                {
                    Id = rt.Id,
                    Name = rt.Name,
                    Description = rt.Description,
                    Capacity = rt.Capacity,
                    Quantity = availableRooms.Count,
                    Price = rt.Price,
                    DateCreated = rt.DateCreated,
                    RoomTypeImages = rt.RoomTypeImages,
                    Rooms = availableRooms
                };
            }).ToList();

            var model = new CreateReservationViewModel
            {
                RoomTypeList = roomTypeWithAvailability,
                UsersId = null
            };

            return View(model);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public IActionResult UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Id) || string.IsNullOrEmpty(request.Status))
            {
                return Json(new { success = false, message = "Invalid request." });
            }

            try
            {
                // find reservation by ID
                var reservation = db.Reservation
                    .Include(r => r.Room)
                    .ThenInclude(room => room.RoomType)
                    .FirstOrDefault(r => r.Id == request.Id);

                if (reservation == null)
                {
                    return Json(new { success = false, message = "Reservation not found." });
                }

                // no updates if the current status is "Completed" or "Canceled"
                if (reservation.Status == "Completed" || reservation.Status == "Canceled")
                {
                    return Json(new { success = false, message = "This reservation cannot be updated." });
                }

                // updt the reservation status
                reservation.Status = request.Status;
                db.Reservation.Update(reservation);

                // see if a transaction exists
                var transaction = db.Transaction
                    .Include(t => t.Users) // Ensure Users navigation property is loaded
                    .Include(t => t.Reservation)
                    .ThenInclude(r => r.Room)
                    .ThenInclude(room => room.RoomType)
                    .FirstOrDefault(t => t.ReservationId == reservation.Id);

                if (transaction != null)
                {
                    // updt the transaction status
                    transaction.Status = request.Status.ToLower();
                    transaction.PaymentDate = request.Status == "Completed" ? DateTime.Now : transaction.PaymentDate;
                    db.Transaction.Update(transaction);
                }
                else if (request.Status == "Completed")
                {
                    // new transaction for admin created guest reservations
                    transaction = new Models.Transaction
                    {
                        Id = GenerateNextTransactionId(),
                        PaymentMethod = "Cash",
                        Amount = reservation.TotalPrice,
                        PaymentDate = DateTime.Now,
                        Status = "completed",
                        DateCreated = DateTime.Now,
                        UsersId = reservation.UsersId, // for guest reservations
                        ReservationId = reservation.Id
                    };

                    db.Transaction.Add(transaction);
                }

                System.Diagnostics.Debug.WriteLine($"Reservation ID: {reservation.Id}, Status: {reservation.Status}, UsersId: {reservation.UsersId}, UserName: {reservation.UserName}, UserEmail: {reservation.UserEmail}");
                System.Diagnostics.Debug.WriteLine($"Transaction ID: {transaction?.Id}, Status: {transaction?.Status}, Users: {transaction?.Users?.Name}, PaymentMethod: {transaction?.PaymentMethod}");

                // Send invoice email 
                if (request.Status == "Completed")
                {
                    var userEmail = reservation.UserEmail ?? transaction.Users?.Email;
                    var userName = reservation.UserName ?? transaction.Users?.Name;

                    if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(userName))
                    {
                        var pdfGenerator = new PdfGenerator();
                        var pdfBytes = pdfGenerator.GenerateInvoice(transaction);

                        hp.SendInvoiceEmail(
                            userEmail,
                            userName,
                            pdfBytes,
                            transaction.Id
                        );
                    }
                }

                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateStatus: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while updating the status." });
            }
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public IActionResult UpdateStatusBulk([FromBody] UpdateStatusRequest request)
        {
            if (request.ReservationIds == null || !request.ReservationIds.Any() || string.IsNullOrEmpty(request.Status))
            {
                return Json(new { success = false, message = "Invalid request. Please select reservations and specify a valid status." });
            }

            try
            {
                var reservationsToUpdate = db.Reservation
                    .Include(r => r.Room)
                    .ThenInclude(room => room.RoomType)
                    .Where(r => request.ReservationIds.Contains(r.Id) && r.Status == "Pending")
                    .ToList();

                if (!reservationsToUpdate.Any())
                {
                    return Json(new { success = false, message = "No eligible reservations found for the selected IDs." });
                }

                int updatedCount = 0;
                foreach (var reservation in reservationsToUpdate)
                {
                    reservation.Status = request.Status;
                    db.Reservation.Update(reservation);

                    var transaction = db.Transaction
                        .FirstOrDefault(t => t.ReservationId == reservation.Id);

                    if (transaction != null)
                    {
                        transaction.Status = request.Status.ToLower();
                        transaction.PaymentDate = request.Status == "Completed" ? DateTime.Now : transaction.PaymentDate;
                        db.Transaction.Update(transaction);
                    }
                    else if (request.Status == "Completed")
                    {
                        transaction = new Models.Transaction
                        {
                            Id = GenerateNextTransactionId(),
                            PaymentMethod = "Cash",
                            Amount = reservation.TotalPrice,
                            PaymentDate = DateTime.Now,
                            Status = "completed",
                            DateCreated = DateTime.Now,
                            UsersId = reservation.UsersId,
                            ReservationId = reservation.Id
                        };
                        db.Transaction.Add(transaction);
                    }

                    if (request.Status == "Completed")
                    {
                        var userEmail = reservation.UserEmail ?? transaction.Users?.Email;
                        var userName = reservation.UserName ?? transaction.Users?.Name;

                        if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(userName))
                        {
                            var pdfGenerator = new PdfGenerator();
                            var pdfBytes = pdfGenerator.GenerateInvoice(transaction);

                            hp.SendInvoiceEmail(userEmail, userName, pdfBytes, transaction.Id);
                        }
                    }

                    updatedCount++;
                }

                db.SaveChanges();

                return Json(new { success = true, message = $"{updatedCount} reservations updated successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateStatusBulk: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while updating the status." });
            }
        }




        [Authorize(Roles = "Customer")]
        [HttpPost]
        public IActionResult ExtendReservation(string reservationId, DateTime newCheckOutDate)
        {
            var reservation = db.Reservation.Include(r => r.Room).FirstOrDefault(r => r.Id == reservationId);
            if (reservation == null)
            {
                return Json(new { success = false, message = "Reservation not found." });
            }

            if (newCheckOutDate <= reservation.CheckOutDate)
            {
                return Json(new { success = false, message = "New check-out date must be later than the current check-out date." });
            }

            var roomTypeId = reservation.Room.RoomTypeId; // Assuming Room has RoomTypeId
            bool isAvailable = IsRoomAvailable(roomTypeId, reservation.CheckOutDate, newCheckOutDate);

            if (!isAvailable)
            {
                // alternative dates
                var alternatives = GetAlternativeDates(roomTypeId, reservation.CheckOutDate, newCheckOutDate);
                return Json(new
                {
                    success = false,
                    message = "The room is not available for the extended dates.",
                    alternatives = alternatives
                });
            }

            // fetch the RoomType and calculate nightly rate
            var roomType = db.RoomType.FirstOrDefault(rt => rt.Id == roomTypeId);
            if (roomType == null)
            {
                return Json(new { success = false, message = "Room type not found." });
            }

            decimal nightlyRate = roomType.Price;

            // calculate additional charges for the extension
            int additionalDays = (newCheckOutDate - reservation.CheckOutDate).Days;
            decimal additionalCharges = additionalDays * nightlyRate;
            decimal tax = additionalCharges * 0.06m; // Example: 6% SST
            decimal totalAdditionalCharges = additionalCharges + tax;

            //  new reservation record for the extension
            var newReservation = new Reservation
            {
                Id = GenerateNextReservationId(),
                CheckInDate = reservation.CheckOutDate,
                CheckOutDate = newCheckOutDate,
                TotalPrice = totalAdditionalCharges,
                Status = "Pending",
                DateCreated = DateTime.Now,
                UserName = reservation.UserName,
                UsersId = reservation.UsersId,
                UserEmail = reservation.UserEmail,
                RoomId = reservation.RoomId
            };

            db.Reservation.Add(newReservation);
            db.SaveChanges();

            // new reservation in session for payment processing
            HttpContext.Session.Set("ReservationDetails", newReservation);

            // redirect to Payment page
            return Json(new
            {
                success = true,
                message = "Reservation extension requires payment confirmation.",
                redirectUrl = Url.Action("Payment", "Reservation")
            });
        }


        [HttpPost]
        public JsonResult ValidateExtendReservationDate(string reservationId, DateTime newCheckOutDate)
        {
            // fetch the reservation by ID
            var reservation = db.Reservation.Include(r => r.Room).FirstOrDefault(r => r.Id == reservationId);
            if (reservation == null)
            {
                return Json(new { isValid = false, message = "Reservation not found." });
            }

            if (newCheckOutDate <= DateTime.Today)
            {
                return Json(new { isValid = false, message = "The new check-out date must be after today's date." });
            }

            if (newCheckOutDate <= reservation.CheckOutDate)
            {
                return Json(new { isValid = false, message = "The new check-out date must be after the current check-out date." });
            }

            // check room availability
            var roomTypeId = reservation.Room.RoomTypeId; // Assuming Room has RoomTypeId
            bool isAvailable = IsRoomAvailable(roomTypeId, reservation.CheckOutDate, newCheckOutDate);

            if (!isAvailable)
            {
                // fetch alternative dates
                var alternatives = GetAlternativeDates(roomTypeId, reservation.CheckOutDate, newCheckOutDate);
                return Json(new
                {
                    isValid = false,
                    message = "The room is not available for the extended dates.",
                    alternatives = alternatives
                });
            }

            // return success if all validations pass
            return Json(new { isValid = true, message = "The selected date is valid." });
        }



        [Authorize(Roles = "Customer")]
        [HttpPost]
        public IActionResult CancelReservation(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return Json(new { success = false, message = "Invalid transaction ID." });
            }

            var transaction = db.Transaction
                .Include(t => t.Reservation)
                .FirstOrDefault(t => t.Id == transactionId);

            var reservation = db.Reservation.FirstOrDefault(r => r.Id == transaction.ReservationId);

            if (transaction == null || transaction.Status.ToLower() != "pending")
            {
                return Json(new { success = false, message = "Transaction not found or already processed." });
            }

            try
            {
                // updt the transaction status to "Canceled"
                transaction.Status = "Canceled";
                reservation.Status = "Canceled";
                db.SaveChanges();

                return Json(new { success = true, message = "Reservation canceled successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error canceling transaction: {ex.Message}");
                return Json(new { success = false, message = "Failed to cancel the reservation. Please try again later." });
            }
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public IActionResult PayReservation(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return Json(new { success = false, message = "Invalid transaction ID." });
            }

            var transaction = db.Transaction
                .Include(t => t.Reservation)
                .ThenInclude(r => r.Room)
                .ThenInclude(room => room.RoomType)
                .FirstOrDefault(t => t.Id == transactionId);

            if (transaction == null)
            {
                return Json(new { success = false, message = "Transaction not found." });
            }

            try
            {
                // transaction details in session
                HttpContext.Session.Set("TransactionId", transaction.Id);
                HttpContext.Session.Set("ReservationDetails", transaction.Reservation);

                // redirect to payment page
                var paymentUrl = Url.Action("Payment", "Reservation");
                return Json(new { success = true, redirectUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PayReservation: {ex.Message}");
                return Json(new { success = false, message = "Failed to process payment. Please try again later." });
            }
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Checkout()
        {
            var reservation = HttpContext.Session.Get<Reservation>("ReservationDetails");
            var selectedCapacity = HttpContext.Session.Get<int?>("SelectedCapacity");

            if (reservation == null)
            {
                return RedirectToAction("RoomTypeDetails", "Home");
            }


            var room = db.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.Id == reservation.RoomId);
            if (room == null || room.RoomType == null)
            {
                return RedirectToAction("RoomTypeDetails", "Home");
            }
 
            var checkoutViewModel = new CheckoutViewModel
            {
                UsersId = reservation.UsersId,
                UserName = reservation.UserName,
                UserEmail = reservation.UserEmail,
                RoomId = reservation.RoomId,
                RoomType = room.RoomType.Name,
                RoomPrice = room.RoomType.Price,
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                TotalPrice = Math.Round(reservation.TotalPrice, 2),
                SelectedCapacity = selectedCapacity,
            };


            return View(checkoutViewModel);
        }


        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmReservation(CreateReservationViewModel model, string action)
        {
            if (action == "Cancel")
            {
                return RedirectToAction("Index", "Home");
            }

            var reservationDetails = HttpContext.Session.Get<Reservation>("ReservationDetails");
            var selectedCapacity = HttpContext.Session.Get<int?>("SelectedCapacity");

            if (reservationDetails == null)
            {
                System.Diagnostics.Debug.WriteLine("Reservation details not found in session.");
                return RedirectToAction("RoomTypeDetails", "Home", new { id = model.RoomId });
            }

            // use the reservation details to create the reservation in the database
            var reservation = new Reservation
            {
                Id = GenerateNextReservationId(),
                RoomId = reservationDetails.RoomId,
                UsersId = reservationDetails.UsersId,
                UserName = reservationDetails.UserName,
                UserEmail = reservationDetails.UserEmail,
                CheckInDate = reservationDetails.CheckInDate,
                Status = "Pending",
                CheckOutDate = reservationDetails.CheckOutDate,
                TotalPrice = Math.Round(reservationDetails.TotalPrice, 2),
                DateCreated = DateTime.Now
            };

            HttpContext.Session.Set("ReservationDetails", reservation);

            // reservation to the database and save changes
            db.Reservation.Add(reservation);
            db.SaveChanges();

            // confirming the reservation, redirect to the payment page
            return RedirectToAction("Payment");
        }




        [Authorize(Roles = "Customer")]
        public IActionResult Payment()
        {
            //  reservation details from session
            var transactionId = HttpContext.Session.Get<string>("TransactionId");
            var reservation = HttpContext.Session.Get<Reservation>("ReservationDetails");
            var selectedCapacity = HttpContext.Session.Get<int?>("SelectedCapacity");

            Models.Transaction transaction = null;
            if (!string.IsNullOrEmpty(transactionId))
            {
                transaction = db.Transaction.FirstOrDefault(t => t.Id == transactionId);
            }

            // reservation is not found in session, redirect to profile page
            if (reservation == null)
            {
                return RedirectToAction("Profile", "Home");
            }

            // handle missing SelectedCapacity
            if (selectedCapacity == null)
            {
                selectedCapacity = InferCapacity(reservation);
                HttpContext.Session.Set("SelectedCapacity", selectedCapacity);
            }

            // fetch room details to fill the form
            var room = db.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.Id == reservation.RoomId);
            if (room == null || room.RoomType == null)
            {
                return RedirectToAction("Profile", "Home");
            }

            // CAlculate 
            var daysStayed = (reservation.CheckOutDate - reservation.CheckInDate).Days;

            var totalRoomPrice = room.RoomType.Price * daysStayed;

            var taxAmount = totalRoomPrice * 0.06m; //tax

            var totalPriceWithTax = totalRoomPrice + taxAmount;

            var roundedRoomPrice = Math.Round(totalRoomPrice, 2);
            var roundedTax = Math.Round(taxAmount, 2);
            var roundedTotalPriceWithTax = Math.Round(totalPriceWithTax, 2);
            var member = membershipService?.GetMember(reservation.UsersId);

            List<Rewards> rewardsList = new List<Rewards>(); // Initialize the rewardsList

            if (member != null)
            {
                var memberRewards = membershipRewardsService.GetAllByMM(member.Id);

                // Get the rewards directly as objects
                var rewardIds = memberRewards.Select(mr => mr.RewardId).ToList();
                rewardsList = db.Rewards.Where(r => rewardIds.Contains(r.Id)).ToList();
            }
            var roomTypeImage = db.RoomTypeImages
                .Where(img => img.RoomTypeId == room.RoomTypeId)
                .Select(img => img.Name) // Assuming `Name` holds the image path or name
                .FirstOrDefault();
            // view model to pass to the Payment page
            var paymentViewModel = new CheckoutViewModel
            {
                UsersId = reservation.UsersId,
                UserName = reservation.UserName,
                UserEmail = reservation.UserEmail,
                RoomId = reservation.RoomId,
                RoomType = room.RoomType.Name,
                RoomPrice = room.RoomType.Price,
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                TotalPrice = roundedRoomPrice,
                TaxAmount = roundedTax,
                TotalRoomPrice = roundedTotalPriceWithTax,
                SelectedCapacity = selectedCapacity,
                SelectedPaymentMethod = transaction?.PaymentMethod,
                userMember = member,
                rewardsList = rewardsList ?? new List<Rewards>(),// Pass an empty list if rewardsList is null,
                Images = roomTypeImage,
            };

            return View(paymentViewModel);
        }

        private int? InferCapacity(Reservation reservation)
        {
            var room = db.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.Id == reservation.RoomId);
            if (room?.RoomType != null)
            {
                return room.RoomType.Capacity;
            }

            return null;
        }

        private int CalculatePoints(decimal discountedPrice)
        {
            const int pointConversionRate = 10; // 1 point for every $10
            return (int)(discountedPrice / pointConversionRate);
        }

        private decimal RewardDiscountAmount(CheckoutViewModel model, decimal totalPrice)
        {
            // Calculate reward discount
            decimal rewardDiscount = (decimal)(totalPrice * (model.rewardDiscount / 100));
            decimal discountedPrice = totalPrice - rewardDiscount;

            Console.WriteLine("Before Discount : " + totalPrice);
            Console.WriteLine("Reward Discount : " + rewardDiscount);
            Console.WriteLine("After Discount : " + discountedPrice);

            // Deduct rewards and add points
            membershipRewardsService.rewardMinus(model.rewardId, model.memberId);
            int earnedPoints = CalculatePoints(discountedPrice);
            membershipService.AddPointsAndLoyalty(model.memberId, earnedPoints);

            Console.WriteLine("Earned Points : " + earnedPoints);

            return rewardDiscount;
        }

        private decimal MemberDiscountAmount(CheckoutViewModel model, decimal totalPrice)
        {
            int levelDisocuntRate = 0;

            if (model.memberId != null)
            {
                if (model.memberLevel.Equals("Basic"))
                {
                    levelDisocuntRate = 5;
                    Console.WriteLine(levelDisocuntRate);
                }
                else if (model.memberLevel.Equals("Platinum"))
                {
                    levelDisocuntRate = 10;
                    Console.WriteLine(levelDisocuntRate);
                }
                else
                {
                    levelDisocuntRate = 20;
                    Console.WriteLine(levelDisocuntRate);
                }
            }
            if (levelDisocuntRate > 0)
            {
                decimal memberDiscount = totalPrice * ((decimal)levelDisocuntRate / 100);

                Console.WriteLine("Before Discount : " + totalPrice);

                Console.WriteLine("Member Discount : " + memberDiscount);

                return memberDiscount;
            }

            return 0;
        }

        //public IActionResult Payment()
        //{
        //    // Retrieve the reservation details from session
        //    var transactionId = HttpContext.Session.Get<string>("TransactionId");
        //    var reservation = HttpContext.Session.Get<Reservation>("ReservationDetails");
        //    var selectedCapacity = HttpContext.Session.Get<int?>("SelectedCapacity");

        //    Models.Transaction transaction = null;
        //    if (!string.IsNullOrEmpty(transactionId))
        //    {
        //        transaction = db.Transaction.FirstOrDefault(t => t.Id == transactionId);
        //    }

        //    // If reservation is not found in session, redirect to checkout page
        //    if (reservation == null)
        //    {
        //        return RedirectToAction("Checkout");
        //    }

        //    // Fetch room details to fill the form
        //    var room = db.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.Id == reservation.RoomId);
        //    if (room == null || room.RoomType == null)
        //    {
        //        return RedirectToAction("Checkout");
        //    }

        //    // Calculate the number of days between CheckInDate and CheckOutDate
        //    var daysStayed = (reservation.CheckOutDate - reservation.CheckInDate).Days;

        //    // Calculate the total room price (Room Price * Days Stayed)
        //    var totalRoomPrice = room.RoomType.Price * daysStayed;

        //    // Calculate tax (6%) on the total room price
        //    var taxAmount = totalRoomPrice * 0.06m; // 6% tax

        //    // Calculate the final total (Room Price + Tax)
        //    var totalPriceWithTax = totalRoomPrice + taxAmount;

        //    // Round the room price and tax to two decimal places
        //    var roundedRoomPrice = Math.Round(totalRoomPrice, 2);
        //    var roundedTax = Math.Round(taxAmount, 2);
        //    var roundedTotalPriceWithTax = Math.Round(totalPriceWithTax, 2);



        //    // Create a view model to pass to the Payment page
        //    var paymentViewModel = new CheckoutViewModel
        //    {
        //        UsersId = reservation.UsersId,
        //        UserName = reservation.UserName,
        //        UserEmail = reservation.UserEmail,
        //        RoomId = reservation.RoomId,
        //        RoomType = room.RoomType.Name, // Room Category name
        //        RoomPrice = room.RoomType.Price,
        //        CheckInDate = reservation.CheckInDate,
        //        CheckOutDate = reservation.CheckOutDate,
        //        TotalPrice = roundedRoomPrice,  // Total price after tax
        //        TaxAmount = roundedTax,  // Separate tax amount
        //        TotalRoomPrice = roundedTotalPriceWithTax,
        //        SelectedCapacity = selectedCapacity,
        //        SelectedPaymentMethod = transaction?.PaymentMethod // Pre-fill if transaction exists
        //    };

        //    return View(paymentViewModel);


        //}



        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(CheckoutViewModel model)
        {
            var transactionId = HttpContext.Session.Get<string>("TransactionId");
            var reservationDetails = HttpContext.Session.Get<Reservation>("ReservationDetails");

            decimal memberDiscount = 0;
            if (model.memberId != null)
            {
                memberDiscount = MemberDiscountAmount(model, reservationDetails.TotalPrice);
            }

            decimal rewardDiscount = 0;
            if (model.rewardId != null)
            {
                rewardDiscount = RewardDiscountAmount(model, reservationDetails.TotalPrice);
            }

            decimal discountPrice = memberDiscount + rewardDiscount;

            Console.WriteLine(discountPrice);

            reservationDetails.TotalPrice -= discountPrice;

            Console.WriteLine(reservationDetails.TotalPrice);


            if (reservationDetails == null)
            {
                System.Diagnostics.Debug.WriteLine("Reservation details not found in session.");
                return RedirectToAction("Checkout", "Reservation");
            }

            System.Diagnostics.Debug.WriteLine($"Selected Payment Method: {model.SelectedPaymentMethod}");

            Models.Transaction transaction = null;

            if (!string.IsNullOrEmpty(transactionId))
            {
                transaction = db.Transaction.FirstOrDefault(t => t.Id == transactionId);

                // transaction ID matches the current reservation
                if (transaction != null && transaction.ReservationId != reservationDetails.Id)
                {
                    // missmatch means a new reservation; clear the session transaction ID
                    transactionId = null;
                    transaction = null;
                    HttpContext.Session.Remove("TransactionId");
                }
            }

            bool isNewReservation = transaction == null;


            if (model.SelectedPaymentMethod == "Cash")
            {
                if (transaction == null)
                {
                    transaction = new Models.Transaction
                    {
                        Id = GenerateNextTransactionId(),
                        PaymentMethod = "Cash",
                        Amount = reservationDetails.TotalPrice,
                        PaymentDate = null,
                        Status = "Pending",
                        DateCreated = DateTime.Now,
                        UsersId = reservationDetails.UsersId,
                        ReservationId = reservationDetails.Id
                    };
                    db.Transaction.Add(transaction);

                    HttpContext.Session.Set("TransactionId", transaction.Id);


                }
                else
                {
                    transaction.PaymentMethod = "Cash";
                    transaction.Status = "Pending";
                    transaction.PaymentDate = DateTime.Now;
                    db.Transaction.Update(transaction);
                }
                db.SaveChanges();
                HttpContext.Session.Remove("ReservationDetails");
                if (isNewReservation)
                {
                    TempData["SuccessMessage"] = "Your reservation has been confirmed!";
                    return RedirectToAction("Profile", "Account");
                }
                else
                {
                    TempData["SuccessMessage"] = "Your reservation has been confirmed!";
                    return RedirectToAction("Profile", "Account");
                }
            }
            else if (model.SelectedPaymentMethod == "PayPal")
            {
                // PayPal payment URL and redirect user
                var paymentUrl = CreatePaypalPayment(reservationDetails, model);
                return Redirect(paymentUrl);
            }
            else if (model.SelectedPaymentMethod == "Stripe")
            {
                return await CreateStripeSession(model);
            }

            return RedirectToAction("Checkout", "Reservation");
        }

        //paypal handling
        [Authorize(Roles = "Customer")]
        private string CreatePaypalPayment(Reservation reservationDetails, CheckoutViewModel model)
        {
            try
            {
                // set up PayPal API context
                var clientId = _configuration.GetValue<string>("PayPal:Key");
                var clientSecret = _configuration.GetValue<string>("PayPal:Secret");
                var mode = _configuration.GetValue<string>("PayPal:mode");

                APIContext apiContext = PaypalConfiguration.GetAPIContext(clientId, clientSecret, mode);

                // payment details
                var itemList = new ItemList()
                {
                    items = new List<Item>()
            {
                new Item()
                {
                    name = "Hotel Room Reservation",
                    currency = "MYR",
                    price = reservationDetails.TotalPrice.ToString("F2"),
                    quantity = "1"
                }
            }
                };

                var payer = new Payer() { payment_method = "paypal" };

                var redirectUrls = new RedirectUrls()
                {
                    cancel_url = this.Url.Action("PaymentCancelled", "Reservation", null, Request.Scheme),
                    return_url = this.Url.Action("PaymentSuccess", "Reservation", null, Request.Scheme)
                };

                var payment = new Payment()
                {
                    intent = "sale",
                    payer = payer,
                    transactions = new List<Transaction>()
            {
                new Transaction()
                {
                    amount = new Amount()
                    {
                        currency = "MYR",
                        total = reservationDetails.TotalPrice.ToString("F2")
                    },
                    description = "Hotel Room Reservation",
                    item_list = itemList
                }
            },
                    redirect_urls = redirectUrls
                };

                // creates payment
                var createdPayment = payment.Create(apiContext);
                var approvalUrl = createdPayment.links.FirstOrDefault(link => link.rel == "approval_url")?.href;

                if (string.IsNullOrEmpty(approvalUrl))
                {
                    throw new Exception("Approval URL not found.");
                }

                return approvalUrl;
            }
            catch
            {
                throw;
            }
        }

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult PaymentSuccess(string paymentId, string PayerID)
        {
            try
            {
                // reservation and transaction details from session
                var reservationDetails = HttpContext.Session.Get<Reservation>("ReservationDetails");
                var transactionId = HttpContext.Session.Get<string>("TransactionId");

                if (reservationDetails == null)
                {
                    return RedirectToAction("Checkout", "Reservation");
                }

                var clientId = _configuration.GetValue<string>("PayPal:Key");
                var clientSecret = _configuration.GetValue<string>("PayPal:Secret");
                var mode = _configuration.GetValue<string>("PayPal:mode");

                // paypal API context
                APIContext apiContext = PaypalConfiguration.GetAPIContext(clientId, clientSecret, mode);

                // capture payment
                var capture = Capture.Get(apiContext, paymentId);
                if (capture.state != "completed")
                {
                    throw new InvalidOperationException("Capture is not completed.");
                }

                Models.Transaction transaction = null;

                // see for an existing transaction
                if (!string.IsNullOrEmpty(transactionId))
                {
                    transaction = db.Transaction
                        .Include(t => t.Users)
                        .Include(t => t.Reservation)
                        .ThenInclude(r => r.Room)
                        .ThenInclude(room => room.RoomType)
                        .FirstOrDefault(t => t.Id == transactionId);
                }

                // see the transaction linked to the current reservation
                if (transaction == null)
                {
                    transaction = db.Transaction
                        .Include(t => t.Reservation)
                        .ThenInclude(r => r.Room)
                        .ThenInclude(room => room.RoomType)
                        .FirstOrDefault(t => t.ReservationId == reservationDetails.Id);
                }

                if (transaction == null)
                {
                    // see reservation explicitly to populate navigation property
                    var reservation = db.Reservation
                        .Include(r => r.Room)
                        .ThenInclude(room => room.RoomType)
                        .FirstOrDefault(r => r.Id == reservationDetails.Id);

                    if (reservation == null)
                    {
                        throw new Exception($"Reservation with ID {reservationDetails.Id} not found in the database.");
                    }

                    // new transaction record
                    transaction = new Models.Transaction
                    {
                        Id = GenerateNextTransactionId(),
                        PaymentMethod = "PayPal",
                        Amount = reservationDetails.TotalPrice,
                        PaymentDate = DateTime.Now,
                        Status = "completed",
                        DateCreated = DateTime.Now,
                        UsersId = reservationDetails.UsersId,
                        ReservationId = reservationDetails.Id,
                        Users = db.Users.FirstOrDefault(u => u.Id == reservationDetails.UsersId),
                        Reservation = reservation
                    };

                    if (transaction.Users == null)
                    {
                        throw new Exception("Users information is required to generate the invoice.");
                    }

                    reservation.Status = "Completed";
                    db.Reservation.Update(reservation);

                    db.Transaction.Add(transaction);
                }
                else
                {
                    transaction.PaymentMethod = "PayPal";
                    transaction.PaymentDate = DateTime.Now;
                    transaction.Status = "completed";

                    var reservation = transaction.Reservation;
                    if (reservation != null)
                    {
                        reservation.Status = "Completed";
                        db.Reservation.Update(reservation);
                    }

                    db.Transaction.Update(transaction);
                }

                // Send the PDF to email
                var pdfGenerator = new PdfGenerator();
                var pdfBytes = pdfGenerator.GenerateInvoice(transaction);

                hp.SendInvoiceEmail(
                    reservationDetails.UserEmail,
                    reservationDetails.UserName,
                    pdfBytes,
                    transaction.Id
                );

                db.SaveChanges();

                // Clear session 
                HttpContext.Session.Remove("ReservationDetails");
                HttpContext.Session.Remove("TransactionId");

                // redirection target
                if (string.IsNullOrEmpty(transactionId))
                {
                    TempData["SuccessMessage"] = "Your reservation has been confirmed!";
                    return RedirectToAction("Profile", "Account");
                }
                else
                {
                    TempData["SuccessMessage"] = "Your reservation has been confirmed!";
                    return RedirectToAction("Profile", "Account");
                }
            }
            catch (Exception)
            {
                return RedirectToAction("PaymentFailed");
            }
        }

        [Authorize(Roles = "Customer")]
        public IActionResult PaymentFailed(string error)
        {
            ViewBag.ErrorMessage = error;
            return View("PaymentFailed");
        }

        //stripe handling
        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> CreateStripeSession([FromBody] CheckoutViewModel model)
        {
            try
            {
                decimal memberDiscount = 0;
                if (model.memberId != null)
                {
                    memberDiscount = MemberDiscountAmount(model, model.TotalRoomPrice);
                }

                decimal rewardDiscount = 0;
                if (model.rewardId != null)
                {
                    rewardDiscount = RewardDiscountAmount(model, model.TotalRoomPrice);
                }

                decimal discountPrice = memberDiscount + rewardDiscount;

                Console.WriteLine("DiscountPrice : " + discountPrice);

                model.TotalRoomPrice -= discountPrice;

                model.TotalRoomPrice = Math.Round(model.TotalRoomPrice, 2);

                Console.WriteLine(model.TotalRoomPrice);


                System.Diagnostics.Debug.WriteLine($"[DEBUG] RoomId: {model.RoomId}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] TotalRoomPrice: {model.TotalRoomPrice}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] UserName: {model.UserName}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] UserEmail: {model.UserEmail}");

                if (string.IsNullOrEmpty(model.RoomId) || model.TotalRoomPrice <= 0 || string.IsNullOrEmpty(model.UserName) || string.IsNullOrEmpty(model.UserEmail))
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] Invalid data received from the frontend.");
                    return StatusCode(400, new { error = "Invalid data received. Ensure RoomId, TotalRoomPrice, UserName, and UserEmail are correctly passed." });
                }

                StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];

                // Create Stripe session options
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "myr",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Room Reservation (Room ID: {model.RoomId})"
                        },
                        UnitAmountDecimal = model.TotalRoomPrice * 100 // Convert to cents
                    },
                    Quantity = 1
                }
            },
                    Mode = "payment",
                    CustomerEmail = model.UserEmail, // pre-fill customer's email
                    Metadata = new Dictionary<string, string>
            {
                { "CustomerName", model.UserName }, // pass customer full name
                { "RoomId", model.RoomId }, // include additional reservation details
                { "TotalRoomPrice", model.TotalRoomPrice.ToString("F2") }
            },
                    SuccessUrl = Url.Action("StripePaymentSuccess", "Reservation", null, Request.Scheme),
                    CancelUrl = Url.Action("Checkout", "Reservation", null, Request.Scheme)
                };

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Stripe session options created: {JsonConvert.SerializeObject(options)}");

                // create the session
                var service = new SessionService();
                var session = await service.CreateAsync(options);

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Stripe session created successfully. Session ID: {session.Id}");

                // return session ID
                return Json(new { sessionId = session.Id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error creating Stripe session: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet]
        [Authorize(Roles = "Customer")]
        public IActionResult StripePaymentSuccess(string sessionId)
        {
            try
            {
                // reservation and transaction details from session
                var reservationDetails = HttpContext.Session.Get<Reservation>("ReservationDetails");
                var transactionId = HttpContext.Session.Get<string>("TransactionId");

                if (reservationDetails == null)
                {
                    System.Diagnostics.Debug.WriteLine("Reservation details not found in session.");
                    return RedirectToAction("Checkout", "Reservation");
                }

                Models.Transaction transaction = null;

                // Check existing transaction
                if (!string.IsNullOrEmpty(transactionId))
                {
                    transaction = db.Transaction
                        .Include(t => t.Users)
                        .Include(t => t.Reservation)
                        .ThenInclude(r => r.Room)
                        .ThenInclude(room => room.RoomType)
                        .FirstOrDefault(t => t.Id == transactionId);
                }

                // Check transaction linked to the current reservation
                if (transaction == null)
                {
                    transaction = db.Transaction
                        .Include(t => t.Reservation)
                        .ThenInclude(r => r.Room)
                        .ThenInclude(room => room.RoomType)
                        .FirstOrDefault(t => t.ReservationId == reservationDetails.Id);
                }

                if (transaction == null)
                {
                    var reservation = db.Reservation
                        .Include(r => r.Room)
                        .ThenInclude(room => room.RoomType)
                        .FirstOrDefault(r => r.Id == reservationDetails.Id);

                    if (reservation == null)
                    {
                        throw new Exception($"Reservation with ID {reservationDetails.Id} not found in the database.");
                    }

                    transaction = new Models.Transaction
                    {
                        Id = GenerateNextTransactionId(),
                        PaymentMethod = "Stripe",
                        Amount = reservationDetails.TotalPrice,
                        PaymentDate = DateTime.Now,
                        Status = "completed",
                        DateCreated = DateTime.Now,
                        UsersId = reservationDetails.UsersId,
                        ReservationId = reservationDetails.Id,
                        Users = db.Users.FirstOrDefault(u => u.Id == reservationDetails.UsersId),
                        Reservation = reservation
                    };

                    if (transaction.Users == null)
                    {
                        throw new Exception("Users information is required to generate the invoice.");
                    }

                    reservation.Status = "Completed";
                    db.Reservation.Update(reservation);

                    db.Transaction.Add(transaction);
                }
                else
                {
                    // update existing transaction
                    transaction.PaymentMethod = "Stripe";
                    transaction.PaymentDate = DateTime.Now;
                    transaction.Status = "completed";

                    var reservation = transaction.Reservation;
                    if (reservation != null)
                    {
                        reservation.Status = "Completed";
                        db.Reservation.Update(reservation);
                    }

                    db.Transaction.Update(transaction);
                }

                // Send the PDF to email
                var pdfGenerator = new PdfGenerator();
                var pdfBytes = pdfGenerator.GenerateInvoice(transaction);

                hp.SendInvoiceEmail(
                    reservationDetails.UserEmail,
                    reservationDetails.UserName,
                    pdfBytes,
                    transaction.Id
                );

                db.SaveChanges();

                HttpContext.Session.Remove("ReservationDetails");
                HttpContext.Session.Remove("TransactionId");

                if (string.IsNullOrEmpty(transactionId))
                {
                    TempData["SuccessMessage"] = "Your reservation has been confirmed!";
                    return RedirectToAction("Profile", "Account");
                }
                else
                {
                    TempData["SuccessMessage"] = "Your reservation has been confirmed!";
                    return RedirectToAction("Profile", "Account");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Stripe Payment Success Error: {ex.Message}");
                return RedirectToAction("PaymentFailed");
            }
        }



        [HttpPost]
        [Authorize(Roles = "Customer")]
        public IActionResult StripeWebhook()
        {
            var json = new StreamReader(HttpContext.Request.Body).ReadToEnd();
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _configuration["Stripe:WebhookSecret"]
                );

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;

                    var transaction = db.Transaction.FirstOrDefault(t => t.Id == session.Id);
                    if (transaction != null)
                    {
                        transaction.Status = "completed";
                        db.SaveChanges();
                    }
                }
            }
            catch (StripeException e)
            {
                Console.WriteLine($"Stripe webhook error: {e.Message}");
                return BadRequest();
            }

            return Ok();
        }

        [Authorize(Roles = "Admin,Manager")]
        public IActionResult ReserveList(string? searchBar, string? sort, string? dir, string? statusFilter, int page = 1, int pageSize = 5)
        {
            ViewBag.Name = searchBar = searchBar?.Trim() ?? "";
            ViewBag.Status = statusFilter;
            ViewBag.PageSize = pageSize;
            ViewBag.Page = page;

            // Filtering logic
            IQueryable<Reservation> reservationsQuery = db.Reservation
                .Include(r => r.Room)
                .ThenInclude(room => room.RoomType)
                .Include(r => r.Users);

            // Apply search filter (search by RoomName and ReservationId)
            if (!string.IsNullOrEmpty(searchBar))
            {
                System.Diagnostics.Debug.WriteLine($"Applying search filter: {searchBar}");
                reservationsQuery = reservationsQuery.Where(r => r.Room.Name.Contains(searchBar) || r.Id.ToString().Contains(searchBar) || r.Users.Name.Contains(searchBar));
            }

            // Apply status filter (if status is selected)
            if (!string.IsNullOrEmpty(statusFilter))
            {
                System.Diagnostics.Debug.WriteLine($"Applying status filter: {statusFilter}");
                reservationsQuery = reservationsQuery.Where(r => r.Status == statusFilter);
            }

            // Select required data and map to ViewModel
            var reservationViewModels = reservationsQuery
                .Select(r => new
                {
                    Reservation = r,
                    LatestTransaction = db.Transaction
                        .Where(t => t.ReservationId == r.Id)
                        .OrderByDescending(t => t.PaymentDate)
                        .FirstOrDefault()
                })
                .Select(data => new ReservationListViewModel
                {
                    ReservationId = data.Reservation.Id,
                    CheckInDate = data.Reservation.CheckInDate,
                    CheckOutDate = data.Reservation.CheckOutDate,
                    TotalPrice = data.Reservation.TotalPrice,
                    TransactionAmount = data.LatestTransaction != null ? data.LatestTransaction.Amount : 0,
                    Status = data.Reservation.Status,
                    RoomName = data.Reservation.Room.Name,
                    RoomCapacity = data.Reservation.Room.RoomType.Capacity,
                    UserName = data.Reservation.UsersId != null ? data.Reservation.Users.Name : data.Reservation.UserName,
                    UserEmail = data.Reservation.UsersId != null ? data.Reservation.Users.Email : data.Reservation.UserEmail
                })
                .ToList();

            ViewBag.Sort = sort;
            ViewBag.Dir = dir;

            Func<ReservationListViewModel, object> sortFunc = sort switch
            {
                "ReservationId" => r => r.ReservationId,
                "CheckInDate" => r => r.CheckInDate,
                "CheckOutDate" => r => r.CheckOutDate,
                "TotalPrice" => r => r.TotalPrice,
                "Status" => r => r.Status,
                "RoomName" => r => r.RoomName,
                "RoomCapacity" => r => r.RoomCapacity,
                "UserName" => r => r.UserName,
                "UserEmail" => r => r.UserEmail,
                _ => r => r.ReservationId
            };

            // Apply sorting
            var sorted = dir == "des" ? reservationViewModels.OrderByDescending(sortFunc) : reservationViewModels.OrderBy(sortFunc);

            // Paging using xpagelist
            var pagedList = sorted.ToPagedList(page, pageSize);

            System.Diagnostics.Debug.WriteLine($"Total reservations after filtering and paging: {pagedList.Count}");

            if (page < 1)
            {
                return RedirectToAction(nameof(ReserveList), new { searchBar, sort, dir, page = 1, pageSize });
            }

            if (page > pagedList.PageCount && pagedList.PageCount > 0)
            {
                return RedirectToAction(nameof(ReserveList), new { searchBar, sort, dir, page = pagedList.PageCount, pageSize });
            }

            if (Request.IsAjax())
            {
                return PartialView("_ReservationList", pagedList);
            }

            return View(pagedList);
        }

        private HotelRoomReservationSystem.Models.Users GetUserByEmail(string email)
        {
            return db.Users.FirstOrDefault(u => u.Email == email);
        }

        [Authorize(Roles = "Customer")]
        public IActionResult GetTransactions(string status, string search, DateTime? startDate, DateTime? endDate)
        {
            //            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            //            var userId = GetUserByEmail(userEmail).Id;


            //            IQueryable<Models.Transaction> transactions = db.Transaction
            //                .Include(t => t.Reservation)
            //                .ThenInclude(r => r.Room)
            //                .ThenInclude(room => room.RoomType);

            //            ////////////////////////////////
            //            /// Based on the user
            //            ////////////////////////////////
            //            transactions = transactions.Where(t => t.UsersId == userId);

            //            var feedbacks = db.Feedback
            //.Where(f => f.UserId == userId)
            //.Select(f => f.ReservationId)
            //.ToList(); // get the reservation id 


            //            var transactionsWithRatingStatus = transactions
            //.Select(t => new TransactionForProfileVM
            //{
            //Transaction = t,
            //IsRated = feedbacks.Contains(t.ReservationId) // feedback comparison
            //})
            //.ToList();


            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            var userId = GetUserByEmail(userEmail).Id;

            IQueryable<Models.Transaction> transactions = db.Transaction
                .Include(t => t.Reservation)
                .ThenInclude(r => r.Room)
                .ThenInclude(room => room.RoomType);

            // Filter by user
            transactions = transactions.Where(t => t.UsersId == userId);

            // Get feedbacks to check rating status
            var feedbacks = db.Feedback
                .Where(f => f.UserId == userId)
                .Select(f => f.ReservationId)
                .ToList();

            // Add transaction with image and rating status
            var transactionsWithRatingStatus = transactions
                .Select(t => new TransactionForProfileVM
                {
                    Transaction = t,
                    IsRated = feedbacks.Contains(t.ReservationId),
                    RoomTypeImage = db.RoomTypeImages
                        .Where(img => img.RoomTypeId == t.Reservation.Room.RoomTypeId)
                        .Select(img => img.Name) // Assuming `Name` stores the image path
                        .FirstOrDefault() // Get the first image
                })
                .ToList();


            if (!string.IsNullOrEmpty(status))
            {
                // Map tab status to database status
                if (status == "active")
                    status = "Pending";
                else if (status == "past")
                    status = "completed";
                else if (status == "canceled")
                    status = "Canceled";

                //transactions = transactions.Where(t => t.Status.ToLower() == status.ToLower());
                transactionsWithRatingStatus = transactionsWithRatingStatus
            .Where(t => t.Transaction.Status.ToLower() == status.ToLower())
            .ToList();
            }

            if (!string.IsNullOrEmpty(status))
            {
                // Map tab status to database status
                if (status == "active")
                    status = "Pending";
                else if (status == "past")
                    status = "completed";
                else if (status == "canceled")
                    status = "Canceled";

                //transactions = transactions.Where(t => t.Status.ToLower() == status.ToLower());
                transactionsWithRatingStatus = transactionsWithRatingStatus
            .Where(t => t.Transaction.Status.ToLower() == status.ToLower())
            .ToList();
            }
            
            if (!string.IsNullOrEmpty(search))
            {
                transactionsWithRatingStatus = transactionsWithRatingStatus
            .Where(t => t.Transaction.Reservation.Room.RoomType.Name.Contains(search))
            .ToList();
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                transactionsWithRatingStatus = transactionsWithRatingStatus
           .Where(t =>
               t.Transaction.Reservation.CheckInDate >= startDate &&
               t.Transaction.Reservation.CheckOutDate <= endDate)
           .ToList();
            }

            System.Diagnostics.Debug.WriteLine($"GetTransactions: Fetching transactions for status: {status}");

            foreach (var transaction in transactions)
            {
                System.Diagnostics.Debug.WriteLine($"Transaction: Id={transaction.Id}, Status={transaction.Status}, Amount={transaction.Amount}");
            }

            System.Diagnostics.Debug.WriteLine($"GetTransactions: Total records fetched: {transactions.Count()}");


            return PartialView("_TransactionList", transactionsWithRatingStatus);
        }

        //no use
        [HttpGet]
        public IActionResult GetReservationDetails(string reservationId)
        {
            // validate the reservationId
            if (string.IsNullOrEmpty(reservationId))
            {
                return Json(new { success = false, message = "Invalid reservation ID." });
            }

            // reservation including related Room and RoomType
            var reservation = db.Reservation
                .Include(r => r.Room)
                .ThenInclude(room => room.RoomType)
                .FirstOrDefault(r => r.Id == reservationId);

            if (reservation == null)
            {
                return Json(new { success = false, message = "Reservation not found." });
            }

            return Json(new
            {
                success = true,
                checkOutDate = reservation.CheckOutDate.ToString("yyyy-MM-dd")
            });
        }


        //testing purpose
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet]
        public IActionResult GetReservationSchedule(DateTime? startDate, DateTime? endDate, string? roomTypeId, string? userId)
        {
            DateTime start = startDate ?? DateTime.Today.AddMonths(-1);
            DateTime end = endDate ?? DateTime.Today;

            var reservationsQuery = db.Reservation
                .Include(r => r.Users)
                .Include(r => r.Room)
                .Where(r => r.CheckInDate < end && r.CheckOutDate > start);

            if (!string.IsNullOrEmpty(roomTypeId))
            {
                reservationsQuery = reservationsQuery.Where(r => r.Room.RoomTypeId == roomTypeId);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                reservationsQuery = reservationsQuery.Where(r => r.UsersId == userId || r.UserName == userId);
            }

            var reservations = reservationsQuery.ToList();

            var calendarEvents = reservations.Select(r => new
            {
                id = r.Id,
                title = $"Room Name: {r.Room.Name}",
                start = r.CheckInDate.ToString("yyyy-MM-dd"),
                end = r.CheckOutDate.AddDays(+1).ToString("yyyy-MM-dd"),
                color = r.Status == "Pending" ? "orange" :
                        r.Status == "Completed" ? "green" :
                        r.Status == "Canceled" ? "red" : "blue",
                user = new
                {
                    name = r.Users?.Name ?? r.UserName,
                    email = r.Users?.Email ?? r.UserEmail
                }
            }).ToList();

            // Serialize to plain JSON
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            return new JsonResult(calendarEvents, jsonOptions);
        }

        [HttpGet]
        public IActionResult ValidateExtendedDates(string reservationId, DateTime newCheckOutDate)
        {
            if (string.IsNullOrEmpty(reservationId))
            {
                return Json(new { isValid = false, message = "Invalid reservation ID." });
            }

            var reservation = db.Reservation
                .Include(r => r.Room)
                .ThenInclude(room => room.RoomType)
                .FirstOrDefault(r => r.Id == reservationId);

            if (reservation == null)
            {
                return Json(new { isValid = false, message = "Reservation not found." });
            }

            DateTime today = DateTime.Today;

            if (newCheckOutDate <= reservation.CheckOutDate)
            {
                return Json(new { isValid = false, message = "New check-out date must be after the current check-out date." });
            }

            if (newCheckOutDate < today)
            {
                return Json(new { isValid = false, message = "New check-out date cannot be in the past." });
            }

            bool isOverlapping = db.Reservation.Any(r =>
                r.RoomId == reservation.RoomId &&
                r.Status == "Pending" &&
                r.CheckInDate < newCheckOutDate &&
                r.CheckOutDate > reservation.CheckOutDate &&
                r.Id != reservationId);

            if (isOverlapping)
            {
                return Json(new { isValid = false, message = "The room is already booked for the selected extended dates." });
            }

            return Json(new { isValid = true, message = "The new dates are valid." });
        }


        [Authorize(Roles = "Customer")]
        public IActionResult DownloadInvoice(string transactionId)
        {
            var transaction = db.Transaction
                .Include(t => t.Users)
                .Include(t => t.Reservation)
                .ThenInclude(r => r.Room)
                .ThenInclude(room => room.RoomType)
                .FirstOrDefault(t => t.Id == transactionId);

            if (transaction == null)
            {
                return NotFound();
            }

            var pdfGenerator = new PdfGenerator();
            var pdfBytes = pdfGenerator.GenerateInvoice(transaction);

            return File(pdfBytes, "application/pdf", $"Invoice_{transactionId}.pdf");
        }


        [Authorize(Roles = "Admin,Manager")]
        [HttpGet]
        public FileResult ExportReservationsToExcel(string? statusFilter)
        {
            System.Diagnostics.Debug.WriteLine($"ExportReservationsToExcel called with statusFilter: {statusFilter}");

            IQueryable<Reservation> reservationsQuery = db.Reservation
                .Include(r => r.Room)
                .ThenInclude(room => room.RoomType)
                .Include(r => r.Users);

            if (!string.IsNullOrEmpty(statusFilter))
            {
                reservationsQuery = reservationsQuery.Where(r => r.Status == statusFilter);
                System.Diagnostics.Debug.WriteLine($"Filtered query: {reservationsQuery.ToQueryString()}");
            }

            var reservations = reservationsQuery
                .Select(r => new
                {
                    r.Id,
                    r.UserName,
                    r.UserEmail,
                    RoomName = r.Room.Name,
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    TotalPrice = r.TotalPrice,
                    r.Status
                })
                .ToList();

            System.Diagnostics.Debug.WriteLine($"Number of reservations to export: {reservations.Count}");

            string filename = "Reservations.xlsx";
            return GenerateExcel(filename, reservations);
        }

        private FileResult GenerateExcel(string filename, IEnumerable<dynamic> reservations)
        {
            DataTable dataTable = new DataTable("Reservations");
            dataTable.Columns.AddRange(new DataColumn[]
            {
        new DataColumn("Reservation ID"),
        new DataColumn("User Name"),
        new DataColumn("User Email"),
        new DataColumn("Room Name"),
        new DataColumn("Check-In Date"),
        new DataColumn("Check-Out Date"),
        new DataColumn("Total Price"),
        new DataColumn("Status"),
            });

            foreach (var reservation in reservations)
            {
                dataTable.Rows.Add(
                    reservation.Id,
                    reservation.UserName,
                    reservation.UserEmail,
                    reservation.RoomName,
                    reservation.CheckInDate?.ToString("yyyy-MM-dd"),
                    reservation.CheckOutDate?.ToString("yyyy-MM-dd"),
                    $"RM {reservation.TotalPrice:N2}",
                    reservation.Status
                );
            }

            using (XLWorkbook workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(dataTable, "Reservations");

                worksheet.Columns().AdjustToContents();

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.RangeUsed().Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.RangeUsed().Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                worksheet.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Column(6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Column(7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                using (MemoryStream stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        filename
                    );
                }
            }
        }




        private string GenerateNextReservationId()
        {
            // get the highest ReservationId currently in the database
            string maxId = db.Reservation.Max(r => r.Id) ?? "R000"; // default is "R000" if no reservations exist

            // extract the numeric part (like from "R001" -> 1)
            int lastNumber = int.Parse(maxId.Substring(1));  // skip  the "R" part

            // generate the next ID by incrementing the number and formatting it as a 3 digit string
            return "R" + (lastNumber + 1).ToString("D3");  // "R001", "R002",....
        }

        private string GenerateNextTransactionId()
        {
            string maxId = db.Transaction.Max(r => r.Id) ?? "T000";

            int lastNumber = int.Parse(maxId.Substring(1));

            return "T" + (lastNumber + 1).ToString("D3");
        }




    }
}

