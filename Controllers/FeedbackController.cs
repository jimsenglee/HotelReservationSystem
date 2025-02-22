using System.Security.Claims;
using DocumentFormat.OpenXml.InkML;
using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.Models;
using HotelRoomReservationSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using PdfSharp.Snippets.Font;
using SixLabors.ImageSharp;
using X.PagedList.Extensions;

namespace HotelRoomReservationSystem.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly IRoomService roomService;
        private readonly IRoomTypeService roomTypeService;
        private readonly IRoomTypeImageService roomTypeImageService;
        private readonly IReservationService reservationService;
        private readonly IWaitingListService waitingListService;
        private readonly IFeedbackService feedbackService;
        private readonly IUserService userService;
        private readonly IFeedbackMediaService feedbackMediaService;
        private readonly Helper hp;
        public FeedbackController(IRoomService roomService, IRoomTypeService roomTypeService, IRoomTypeImageService roomTypeImageService, Helper hp, IReservationService reservationService, IWaitingListService waitingListService, IFeedbackService feedbackService, IUserService userService, IFeedbackMediaService feedbackMediaService)
        {
            this.roomService = roomService;
            this.roomTypeService = roomTypeService;
            this.roomTypeImageService = roomTypeImageService;
            this.hp = hp;
            this.reservationService = reservationService;
            this.waitingListService = waitingListService;
            this.feedbackService = feedbackService;
            this.userService = userService;
            this.feedbackMediaService = feedbackMediaService;
        }

        [Route("FeedbackManagement")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Index(string? searchBar, string? sort, string? dir, int page = 1, int pageSize = 5)
        {
            if (HttpContext.Session.GetString("SelectRoomType") != null || string.IsNullOrEmpty(HttpContext.Session.GetString("SelectRoomType"))) HttpContext.Session.Remove("SelectRoomType");
            if (pageSize == 0) pageSize = feedbackService.GetAllFeedbacks().Count;

            ViewBag.Name = searchBar = searchBar?.Trim() ?? "";
            ViewBag.PageSize = pageSize;
            ViewBag.Page = page;
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;
            var feedback = string.IsNullOrEmpty(searchBar)
     ? feedbackService.GetAllFeedbacks()
     : feedbackService.GetAllFeedbackById(searchBar);

            var feedbackDetails = feedback.Select(f => new FeedbackDetailsVM
            {
                FeedbackId = f.Id,
                Description = f.Description,
                Rate = f.Rating,
                RoomTypeName = roomTypeService.GetRoomTypeById(roomService.GetRoomById(reservationService.getReservationById(f.ReservationId).RoomId).RoomTypeId).Name,
                RoomName = roomService.GetRoomById(reservationService.getReservationById(f.ReservationId).RoomId).Name,
                UserId = userService.GetUserById(f.UserId).Name,
                Images = feedbackMediaService.GetAllFeedbackMediaByFeedbackId(f.Id),
            }).ToList();

            dir = dir?.ToLower() == "des" ? "des" : "asc";
            // Define sorting function based on the sort parameter
            Func<FeedbackDetailsVM, dynamic> fn = sort switch
            {
                "No" => fm => fm.FeedbackId,
                "Room Type Name" => fm => fm.RoomTypeName,
                "Rating" => fm => fm.Rate,
                "Description" => fm => fm.Description,
                "Room Number" => fm => fm?.RoomName ?? "",
                "User Name" => fm => fm.UserId,
                "Images" => fm => fm.Images,
                _ => fm => fm.FeedbackId // Default sorting column
            };

            var sorted = dir == "des" ? feedbackDetails.OrderByDescending(fn) : feedbackDetails.OrderBy(fn);

            // Paging logic
            if (page < 1) // Ensure page number is valid
            {
                return RedirectToAction(null, new { searchBar, sort, dir, page = 1, pageSize });
            }

            var m = sorted.ToPagedList(page, pageSize); // Set page size = 5

            if (page > m.PageCount && m.PageCount > 0) // Redirect if page exceeds total pages
            {
                return RedirectToAction(null, new { searchBar, sort, dir, page = m.PageCount, pageSize });
            }

            if (Request.IsAjax())
            {
                return PartialView("_FeedbackList", m); // Return only the updated reservation list and pagination controls
            }

            return View("FeedbackManagement", m);
        }

        //[HttpPost]
        //[Route("Feedback/Submit")]
        //[Consumes("multipart/form-data")]
        //public IActionResult SubmitFeedback([FromForm] FeedbackAddVM model)
        //{
        //    if (model.Rating == 0)
        //            return BadRequest("Invalid data.");
        //    //if (model.Images == null) return NotFound("Invalid Image");
        //    //if (model.Comment == null) return BadRequest("Invalid Comment");


        //    // Save the feedback data
        //    // model.Rating, model.Comment, model.Images (uploaded files)

        //    foreach (var file in model.Images)
        //    {
        //        // Save files to the server or a cloud storage
        //        Console.WriteLine(file.FileName);
        //    }

        //    return Ok(new { success = true });
        //}



        [HttpPost]
        [Route("Feedback/Submit")]
        [Authorize(Roles = "Customer")]
        [Consumes("multipart/form-data")]
        public async Task<JsonResult> SubmitFeedback([FromForm] FeedbackAddVM model)
        {
            Console.WriteLine("sadskjdh");
            var rate = double.Parse(Request.Form["Rating"]);
            Console.WriteLine("Rating > " + rate);
            Console.WriteLine($"Incoming Rating: {Request.Form["Rating"]}");
            Console.WriteLine($"Model Rating: {model.Rating}");
            var userId = HttpContext.Session.GetString("UserId");
            if (userId == null)
            {
                var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                userId = userService.GetUserByEmail(userEmail).Id;
            }
            if (rate <= 0)
                return Json(new { message = "Please rate it.", success = false });
            else ModelState.Remove("Rating");

            if (ModelState.IsValid)
            {
                var id = feedbackService.GenerateCode();
                feedbackService.AddFeedback(new Feedback
                {
                    Rating = rate,
                    Description = model.Comment ?? "",
                    DateCreated = DateTime.Now,
                    Id = id,
                    ReservationId = model.ReservationId,
                    UserId = userId,
                });

                if (model.Images != null)
                {
                    var e = hp.ValidateFiles(model.Images);
                    if (e != "")
                    {
                        return Json(new { message = e, success = false });
                    }

                    // Process each image
                    foreach (var file in model.Images)
                    {
                        // Access file metadata
                        string fileName = file.FileName;           // Original file name
                        string contentType = file.ContentType;     // File MIME type (e.g., "image/jpeg")
                        long fileSize = file.Length;              // File size in bytes

                        // Example of saving file info to console
                        Console.WriteLine($"File: {fileName}");
                        Console.WriteLine($"Type: {contentType}");
                        Console.WriteLine($"Size: {fileSize} bytes");

                        // Example of saving file with original name
                        //string uploadPath = Path.Combine("YourUploadDirectory", fileName);
                        fileName = feedbackMediaService.AddImages(file, id);
                        hp.SaveImage(file, "images/Feedback", fileName);
                    }
                    return Json(true);
                }
            }

            return Json(new { message = "Invalid column", success = false });
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [Route("Feedback/Update")]
        [Consumes("multipart/form-data")]
        public async Task<JsonResult> Update([FromForm] FeedbackAddVM model)
        {
            var rate = double.Parse(Request.Form["Rating"]);
            var userId = HttpContext.Session.GetString("UserId");
            if (userId == null)
            {
                var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                userId = userService.GetUserByEmail(userEmail).Id;
            }
            if (rate <= 0)
                return Json(new { message = "Please rate it.", success = false });
            else ModelState.Remove("Rating");

            if (ModelState.IsValid)
            {

                var feedback = feedbackService.GetFeedbackById(Request.Form["FeedbackId"]);
                if (feedback == null) return Json(new { message = "Feedback id are not valid.", success = false });
                feedback.Rating = rate;
                feedback.Description = model.Comment ?? "";

                feedbackService.UpdateFeedback(feedback);

                if (model.Images != null)
                {
                    var e = hp.ValidateFiles(model.Images);
                    if (e != "")
                    {
                        return Json(new { message = e, success = false });
                    }
                    var existFeedbackMedia = feedbackMediaService.GetAllFeedbackMediaByFeedbackId(feedback.Id);
                    if (existFeedbackMedia != null)
                        foreach (var images in existFeedbackMedia)
                            feedbackMediaService.RemoveFeedbackMedia(images);

                    // Process each image
                    foreach (var file in model.Images)
                    {
                        // Access file metadata
                        string fileName = file.FileName;           // Original file name
                        string contentType = file.ContentType;     // File MIME type (e.g., "image/jpeg")
                        long fileSize = file.Length;              // File size in bytes

                        // Example of saving file info to console
                        Console.WriteLine($"File: {fileName}");
                        Console.WriteLine($"Type: {contentType}");
                        Console.WriteLine($"Size: {fileSize} bytes");

                        // Example of saving file with original name
                        //string uploadPath = Path.Combine("YourUploadDirectory", fileName);
                        fileName = feedbackMediaService.AddImages(file, feedback.Id);
                        hp.SaveImage(file, "images/Feedback", fileName);
                    }
                    return Json(true);
                }
            }

            return Json(new { message = "Invalid column", success = false });
        }


        [HttpGet]
        [Authorize(Roles = "Customer")]
        public IActionResult GetSpecFeedback(string reservationId)
        {
            if (reservationId == null) return null;
            var tmpData = new List<string>();
            var feedback = feedbackService.GetFeedbackByReservationId(reservationId.Trim());
            var existReservation = reservationService.getReservationById(reservationId.Trim());
            var room = roomService.GetRoomById(existReservation.RoomId);
            var roomTypeName = roomTypeService.GetRoomTypeById(room.RoomTypeId).Name;
            var images = feedbackMediaService.GetAllFeedbackMediaByFeedbackId(feedback.Id);

            var fbDetails = new FeedbackDetailsVM
            {
                FeedbackId = feedback.Id,
                ReservationId = feedback.ReservationId,
                RoomTypeName = roomTypeName,
                RoomName = room.Name,
                Rate = feedback.Rating,
                Description = feedback.Description.Trim(),
                Images = images
            };

            if (fbDetails == null) return NotFound();

            return Json(fbDetails);
        }
    }
}
