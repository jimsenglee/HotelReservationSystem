using X.PagedList.Extensions;
using HotelRoomReservationSystem.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using HotelRoomReservationSystem.Models.ViewModels;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace HotelRoomReservationSystem.Controllers
{
    public class RoomTypeController : Controller
    {
        private readonly IRoomService roomService;
        private readonly IRoomTypeService roomTypeService;
        private readonly IRoomTypeImageService roomTypeImageService;
        private readonly IReservationService reservationService;
        private readonly IWaitingListService waitingListService;
        private readonly IFeedbackMediaService feedbackMediaService;
        private readonly IUserService userService;
        private readonly IFeedbackService feedbackService;
        private readonly Helper hp;
        private readonly HotelRoomReservationDB db;

        public RoomTypeController(IRoomService roomService, IRoomTypeService roomTypeService, IRoomTypeImageService roomTypeImageService, Helper hp, IReservationService reservationService, IWaitingListService waitingListService, IFeedbackMediaService feedbackMediaService, IUserService userService, IFeedbackService feedbackService, HotelRoomReservationDB context)
        {
            db = context;
            this.roomService = roomService;
            this.roomTypeService = roomTypeService;
            this.roomTypeImageService = roomTypeImageService;
            this.hp = hp;
            this.reservationService = reservationService;
            this.waitingListService = waitingListService;
            this.feedbackMediaService = feedbackMediaService;
            this.userService = userService;
            this.feedbackService = feedbackService;
        }

        [Authorize(Roles = "Admin,Manager")]
        [Route("RoomTypeManagement")]
        public IActionResult Index(string? searchBar, string? sort, string? dir, int page = 1, int pageSize = 5, string status = "All")
        {
            if (HttpContext.Session.GetString("SelectRoomType") != null || string.IsNullOrEmpty(HttpContext.Session.GetString("SelectRoomType"))) HttpContext.Session.Remove("SelectRoomType");
            ViewBag.Name = searchBar = searchBar?.Trim() ?? "";
            ViewBag.PageSize = pageSize;
            ViewBag.Page = page;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;
            var roomType = string.IsNullOrEmpty(searchBar)
     ? roomTypeService.GetAllRoomType()
     : roomTypeService.GetAllRoomTypeById(searchBar);




            var roomTypeDetails = roomType.Select(roomType => new RoomTypeDetailsVM
            {
                RoomTypeId = roomType.Id,
                RoomTypeName = roomType.Name,
                Description = roomType.Description,
                Capacity = roomType.Capacity,
                AvbQuantity = CalculateAvailableRooms(roomType.Id),
                TtlQuantity = roomType.Quantity,
                Price = (double)roomType.Price,
                Status = roomType.Status,
                Images = roomTypeImageService.GetRoomTypeImagesById(roomType.Id),
            }).ToList();

            if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                roomTypeDetails = roomTypeDetails
                    .Where(c => string.Equals(c.Status, status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var allRoomType = roomTypeService.GetAllRoomType();
            foreach (var rt in allRoomType)
            {
                var rooms = roomService.GetAllRoomByCategoryId(rt.Id).Where(rty => rty.Status != "Disabled").ToList();
                rt.Quantity = rooms.Count;

                if (rt.Quantity == 0) rt.Status = "Unavailable";
                else rt.Status = "Available";

                roomTypeService.UpdateRooomType(rt);
            }


            if (pageSize == 0) pageSize = allRoomType.Count;

            dir = dir?.ToLower() == "des" ? "des" : "asc";
            // Define sorting function based on the sort parameter
            Func<RoomTypeDetailsVM, dynamic> fn = sort switch
            {
                "No" => c => c.RoomTypeId,
                "RoomTypeName" => c => c.RoomTypeName,
                "Description" => c => c.Description,
                "Price" => c => c?.Price ?? 0.0,
                "Capacity" => c => c.Capacity,
                "Avb /Total Qty" => c => c.AvbQuantity,
                "Status" => c => c.Status,
                _ => c => c.RoomTypeId // Default sorting column
            };

            var sorted = dir == "des" ? roomTypeDetails.OrderByDescending(fn) : roomTypeDetails.OrderBy(fn);

            // Paging logic
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
                return PartialView("_RoomTypeList", m); // Return only the updated reservation list and pagination controls
            }

            return View("RoomTypeManagement", m);
        }
        private int CalculateAvailableRooms(string roomTypeId)
        {
            // Get all rooms in the specified category
            var roomList = roomService.GetAllRoomByCategoryId(roomTypeId);
            int availableCount = 0;

            foreach (var room in roomList)
            {
                // Check if there are any reservations for this room today
                bool isReserved = reservationService.GetAllReservationByRoomId(room.Id)
                    .Any(reservation => reservation.CheckInDate.Date == DateTime.Now.Date);

                if (!isReserved) // If no reservations found, room is available
                {
                    availableCount++;
                }

                Console.WriteLine($"Room ID: {room.Id}, Reserved: {isReserved}");
            }

            Console.WriteLine("Total Available Rooms Today: " + availableCount);
            return availableCount;
        }

        [Authorize(Roles = "Admin,Manager")]
        [Route("RefreshList")]
        public IActionResult RoomTypeManagement(string? searchBar, string? sort, string? dir, int page = 1, int pageSize = 5, string status = "All")
        {
            if (HttpContext.Session.GetString("SelectRoomType") != null || string.IsNullOrEmpty(HttpContext.Session.GetString("SelectRoomType"))) HttpContext.Session.Remove("SelectRoomType");
            ViewBag.Name = searchBar = searchBar?.Trim() ?? "";
            ViewBag.PageSize = pageSize;
            ViewBag.Page = page;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;
            var roomType = string.IsNullOrEmpty(searchBar)
     ? roomTypeService.GetAllRoomType()
     : roomTypeService.GetAllRoomTypeById(searchBar);




            var roomTypeDetails = roomType.Select(roomType => new RoomTypeDetailsVM
            {
                RoomTypeId = roomType.Id,
                RoomTypeName = roomType.Name,
                Description = roomType.Description,
                Capacity = roomType.Capacity,
                AvbQuantity = CalculateAvailableRooms(roomType.Id),
                TtlQuantity = roomType.Quantity,
                Price = (double)roomType.Price,
                Status = roomType.Status,
                Images = roomTypeImageService.GetRoomTypeImagesById(roomType.Id),
            }).ToList();

            if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                roomTypeDetails = roomTypeDetails
                    .Where(c => string.Equals(c.Status, status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var allRoomType = roomTypeService.GetAllRoomType();
            foreach (var rt in allRoomType)
            {
                var rooms = roomService.GetAllRoomByCategoryId(rt.Id).Where(rty => rty.Status != "Disabled").ToList();
                rt.Quantity = rooms.Count;

                if (rt.Quantity == 0) rt.Status = "Unavailable";

                roomTypeService.UpdateRooomType(rt);
            }


            if (pageSize == 0) pageSize = allRoomType.Count;

            dir = dir?.ToLower() == "des" ? "des" : "asc";
            // Define sorting function based on the sort parameter
            Func<RoomTypeDetailsVM, dynamic> fn = sort switch
            {
                "No" => c => c.RoomTypeId,
                "RoomTypeName" => c => c.RoomTypeName,
                "Description" => c => c.Description,
                "Price" => c => c?.Price ?? 0.0,
                "Capacity" => c => c.Capacity,
                "Avb /Total Qty" => c => c.AvbQuantity,
                "Status" => c => c.Status,
                _ => c => c.RoomTypeId // Default sorting column
            };

            var sorted = dir == "des" ? roomTypeDetails.OrderByDescending(fn) : roomTypeDetails.OrderBy(fn);

            // Paging logic
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
                return PartialView("_RoomTypeList", m); // Return only the updated reservation list and pagination controls
            }

            return View("RoomTypeManagement", m);
        }


        [Authorize(Roles = "Admin,Manager")]
        [Route("RTForm")]
        public IActionResult AddForm()
        {
            //ViewBag.Id = categoryService.GenerateCode();
            return View("RTForm");
        }


        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("CheckAddIdAvailable")]
        public IActionResult CheckAddIdAvailable(string newId, string Exist)
        {
            var existingRoom = roomService.GetRoomById(newId);
            if (existingRoom == null)
            {
                return Json($"Room Id '{newId}' is already in use.");
            }
            return Json(true);
        }

        [Authorize(Roles = "Admin,Manager")]
        public void UploadImages(RoomTypeAddVM model, string categoryId)
        {
            foreach (var file in model.Images)
            {
                //var imageId = roomTypeImageService.GenerateImageId(DateTime.Now);
                var imageName = roomTypeImageService.AddImages(file, categoryId);
                hp.SaveImage(file, "images/RoomType", imageName);
            }
        }


        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("CheckNameAvailability")]
        public JsonResult CheckNameAvailability(string name, string id = null)
        {
            // Check for duplicate name logic
            bool isNameDuplicate = string.IsNullOrEmpty(id)
                ? !roomTypeService.IsSameName(name) // Check by name only
                : roomTypeService.IsSameName(name, id); // Check by name and id

            return Json(isNameDuplicate); // Return true if name is available
        }

        [HttpPost]
        public IActionResult SubmitForm(RoomTypeAddVM vm, List<string> ExistingPreviews, string id = null)
        {
            if (vm != null)
            {
                if (id != null)
                {
                    ModelState.Remove("StartRoom");
                    ModelState.Remove("EndRoom");
                }
                if (vm.Id == null)
                {
                    vm.Id = roomTypeService.GenerateCode();
                    ModelState.Remove("Id");
                }

                if (ModelState.IsValid("Name") && roomTypeService.IsSameName(vm.Name))
                {
                    if (!roomTypeService.IsSameName(vm.Name, vm.Id))
                        ModelState.AddModelError("Name", "Duplicated Name.");
                }

                if (ModelState.IsValid("RoomQuantity"))
                {
                    if (vm.RoomQuantity <= 0)
                    {
                        ModelState.AddModelError("RoomQuantity", "Please ensure you select a range of rooms.");
                    }
                }

                var e = hp.ValidateFiles(vm.Images);
                if (e != "")
                {
                    ModelState.AddModelError("Images", e);
                }

                if (ExistingPreviews != null && ExistingPreviews.Count > 0 && vm.Images == null)
                {
                    vm.ImagePreviews.AddRange(ExistingPreviews);
                }

                if (ModelState.IsValid)
                {
                    if (!roomTypeService.IsSameId(vm.Id))
                    {
                        roomTypeService.AddRoomType(new RoomType
                        {
                            Id = vm.Id,
                            Name = vm.Name,
                            Description = vm.Description,
                            Price = vm.Price,
                            Quantity = vm.RoomQuantity,
                            Capacity = vm.RoomCapacity,
                            DateCreated = DateTime.Now,
                            Status = "Available",
                        });
                        UploadImages(vm, vm.Id);

                        if (vm.RoomQuantity > 0)
                        {
                            var roomUvb = roomService.GetExistingRoom(vm.StartRoom, vm.EndRoom);
                            var roomRange = roomService.GetRange(vm.StartRoom, vm.EndRoom);
                            var roomAvb = roomRange;
                            if (roomUvb != null)
                                roomAvb = roomRange.Except(roomUvb).ToList();

                            foreach (var name in roomAvb)
                            {
                                var roomId = roomService.GenerateCode();
                                roomService.AddRoom(new Rooms
                                {
                                    Id = roomId,
                                    Name = name,
                                    Status = "Available",
                                    DateCreated = DateTime.Now,
                                    RoomTypeId = vm.Id,
                                });
                            }
                        }

                        return RedirectToAction("Index");
                    }
                    else
                    {
                        // Get the data from DB
                        var existRoomType = roomTypeService.GetRoomTypeById(vm.Id);
                        var existingRooms = roomService.GetAllRoomByCategoryId(vm.Id);
                        if (existRoomType != null)
                        {
                            //Update category details
                            existRoomType.Name = vm.Name;
                            existRoomType.Price = vm.Price;
                            existRoomType.Description = vm.Description;
                            existRoomType.Quantity = vm.RoomQuantity;
                            existRoomType.Capacity = vm.RoomCapacity;
                            // Remove current have
                            var imgs = roomTypeImageService.GetRoomTypeImagesById(vm.Id);
                            foreach (var img in imgs)
                            {
                                hp.DeletePhoto(img.Name, "/images/RoomType/");
                            }
                            roomTypeImageService.Remove(vm.Id);

                            // Update new images
                            UploadImages(vm, vm.Id);

                            // Save new category
                            roomTypeService.UpdateRooomType(existRoomType);

                            // If success return back to the list
                            return RedirectToAction("Index");
                        }
                        return View("UpdateForm", vm);
                    }
                }
            }

            // If got error return back the form
            //return RedirectToAction("CForm", vm);
            return View("RTForm", vm);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        [Route("/RoomType/GetRoomTypeDetails")]
        public IActionResult GetRoomTypeDetails(string id)
        {
            try
            {
                // Fetch the room details from your service or repository
                var roomType = roomTypeService.GetRoomTypeById(id);

                if (roomType == null)
                {
                    return NotFound(new { Message = "Room not found." });
                }

                // Return as JSON
                return Json(id);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine(ex);

                // Return a failure response
                return StatusCode(500, new { Message = "An error occurred while fetching room details." });
            }
        }


        [HttpPost]
        public IActionResult StoreSession(string key, string value, string action, string controller)
        {

            HttpContext.Session.SetString(key, value);

            // Redirect to a generic path (e.g., Details page)
            var redirectUrl = Url.Action(action, controller);
            return Json(new { redirectUrl });
        }

        public IActionResult Details()
        {
            try
            {
                string? roomTypeId = HttpContext.Session.GetString("SelectRoomType");
                if (roomTypeId == null) return NotFound();
                // Fetch the room details
                var roomType = roomTypeService.GetRoomTypeById(roomTypeId);

                if (roomType == null)
                    // Redirect to an error page or show a "not found" message
                    return RedirectToAction("Error", new { message = "Room not found." });

                // Fetch related data
                var roomImages = roomTypeImageService.GetRoomTypeImagesNameById(roomTypeId);
                // Map the single room object to the ViewModel
                Console.WriteLine(string.Join(", ", roomImages));
                var roomTypeDetails = new RoomTypeUpdateVM
                {
                    Id = roomType.Id,
                    Description = roomType.Description,
                    Name = roomType.Name,
                    Price = roomType.Price,
                    RoomCapacity = roomType.Capacity,
                    RoomQuantity = roomType.Quantity,
                    ImagePreviews = roomImages
                };


                ViewBag.RoomList = roomService.GetAllRoomByCategoryId(roomTypeId); // Returns List<string>
                ViewBag.RoomRange = null;
                if (ViewBag.RoomList != null)                                                                   // Return the room details to the view
                    ViewBag.RoomRange = roomService.ConvertToRange(ViewBag.RoomList);
                return View("UpdateForm", roomTypeDetails);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine(ex);

                // Redirect to an error page or show a "server error" message
                return RedirectToAction("Error", new { message = "An error occurred while loading the room details." });
            }
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public IActionResult AddRoom([FromBody] RoomRangeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.EndRoom) || string.IsNullOrEmpty(request.StartRoom))
                return Json(new { success = false, message = "Invalid request." });



            // Find the room type by ID (you can use your service/repository layer here)
            var roomType = roomTypeService.GetRoomTypeById(request.Id);
            if (roomType == null)
                return Json(new { success = false, message = "Room type not found." });


            // Update the status
            var roomUvb = roomService.GetExistingRoom(request.StartRoom, request.EndRoom);
            var roomRange = roomService.GetRange(request.StartRoom, request.EndRoom);
            var roomAvb = roomRange;
            if (roomUvb != null)
                roomAvb = roomRange.Except(roomUvb).ToList();


            foreach (var name in roomAvb)
            {
                var roomId = roomService.GenerateCode();
                roomService.AddRoom(new Rooms
                {
                    Id = roomId,
                    Name = name,
                    Status = "Available",
                    DateCreated = DateTime.Now,
                    RoomTypeId = request.Id,
                });
            }
            roomType.Quantity += roomAvb.Count;
            roomTypeService.UpdateRooomType(roomType);

            return Json(new { success = true });
        }


        [HttpGet("CheckStartRoom")]
        public IActionResult CheckStartRoom(string startRooom)
        {
            if (string.IsNullOrEmpty(startRooom))
            {
                return BadRequest("End Room is required.");
            }

            if (startRooom.Length > 4)
            {
                return BadRequest("End Room cannot exceed 4 characters.");
            }

            // Add any additional validation logic here
            if (!Regex.IsMatch(startRooom, "^[A-Za-z0-9]+$")) // Example validation
            {
                return BadRequest("End Room contains invalid characters.");
            }

            return Ok(); // Validation passed
        }

        [HttpGet("CheckEndRoom")]
        public IActionResult CheckEndRoom(string endRoom)
        {
            if (string.IsNullOrEmpty(endRoom))
            {
                return BadRequest("End Room is required.");
            }

            if (endRoom.Length > 4)
            {
                return BadRequest("End Room cannot exceed 4 characters.");
            }

            // Add any additional validation logic here
            if (!Regex.IsMatch(endRoom, "^[A-Za-z0-9]+$")) // Example validation
            {
                return BadRequest("End Room contains invalid characters.");
            }

            return Ok(); // Validation passed
        }

        [HttpGet("CheckValueValidateEndRoom")]
        public JsonResult CheckValueValidateEndRoom(string EndRoom, string StartRoom)
        {
            if (StartRoom != null && EndRoom != null)
            {
                try
                {
                    // Extract the numeric part of the room numbers
                    string startPart = Regex.Match(StartRoom, @"\d+").Value;
                    string endPart = Regex.Match(EndRoom, @"\d+").Value;

                    // Check if numeric parts are found
                    if (string.IsNullOrEmpty(startPart) || string.IsNullOrEmpty(endPart))
                    {
                        return Json("Invalid room format.");
                    }

                    int start = int.Parse(startPart);
                    int end = int.Parse(endPart);

                    // Call room service to check for duplicates
                    var rangeErr = roomService.CheckNameRange(StartRoom, EndRoom);

                    if (!string.IsNullOrEmpty(rangeErr))
                    {
                        return Json(rangeErr); // Return error message                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                
                    }

                    return Json(true); // Validation passed
                }
                catch (FormatException)
                {
                    return Json("Invalid room number format.");
                }
            }

            return Json("Both fields are required.");
        }

        [HttpGet("CheckDuplicateRoom")]
        public JsonResult CheckDuplicateRoom(string startRoom, string endRoom)
        {
            var duplicateRooms = roomService.GetExistingRoom(startRoom, endRoom);
            var allRooms = roomService.GetRange(startRoom, endRoom);
            if (duplicateRooms != null)
            {
                var validRooms = allRooms.Except(duplicateRooms).ToList();
                Console.Write("COunted > " + validRooms.Count);
                return Json(new
                {
                    success = false,
                    message = $"In this range, the following rooms have duplicates: {string.Join(", ", duplicateRooms)}.",
                    validRoomCount = validRooms.Count,
                });
            }

            return Json(new
            {
                success = true,
                validRoomCount = allRooms.Count,
            });
        }


        [HttpGet("GetRoomsByRowId")]
        public JsonResult GetRoomsByRowId(string id, string? require = null)
        {
            var wtData = new List<string>();
            foreach (var wt in waitingListService.GetAllWaitingList())
            {
                if (wt.OrgRoomTypeId == id)
                    wtData.Add(wt.RoomId);
            }

            // Fetch the rooms based on the rowId
            var rooms = roomService.GetAllRoomByCategoryId(id).Where(room => room.Status != "Disabled" && room.Status != "Waiting" && !wtData.Contains(room.Id)).ToList();
            var roomInfoList = new List<List<string>>();

            // Add room.Name and room.Id to the list
            foreach (var room in rooms)
            {
                roomInfoList.Add(new List<string> { room.Id, room.Name });
            }
            // Return null if no rooms found
            if (rooms == null) return Json(null);

            // Convert rooms to a range
            var dataRange = roomService.ConvertToRange(rooms);
            var response = new RoomDataResponseVM
            {
                RoomList = roomInfoList,
                RoomRange = dataRange,
            };

            // Check if the 'require' parameter is provided
            if (require != null)
            {
                // Add RoomTypeList if 'require' is not null
                response.RoomTypeList = roomTypeService.GetAllRoomType()
    .Where(roomType => !roomService.GetAllRoomByCategoryId(id)
        .Select(room => room.RoomTypeId)
        .Contains(roomType.Id))
    .Select(roomType => roomType.Name)
    .ToList();

            }

            // Set JsonSerializerOptions to handle references and max depth
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve, // Enable reference handling to preserve object cycles
                MaxDepth = 32 // You can also adjust the max depth if necessary
            };

            // Return JSON response with options
            return Json(response, options);
        }

        [HttpPost]
        public JsonResult RemoveRoom(List<string> rooms)
        {
            if (rooms == null && !rooms.Any()) return Json(new { success = false, message = "No rooms selected." });
            // Process the selected rooms
            foreach (var room in rooms)
            {
                // Remove the rooms.
            }

            return Json(new { success = true, message = "Rooms saved successfully!" });
        }

        [HttpPost("Remove")]
        public JsonResult Remove(List<string> roomId, string? roomType, string? spec = null)
        {
            Console.WriteLine("Room id > " + string.Join(", ", roomId) + "\nRoom Type: " + roomType + "\nSpec: " + spec);
            var msg = string.Empty;
            if (roomId == null) return Json(new { message = "Please select a room.", success = false });
            var err = "";
            if (roomType != null)
                if (roomTypeService.GetRoomTypeInfoByName(roomType) == null) return Json(new { message = "Not any data founds.", success = false });




            foreach (var r in roomId)
            {
                if (roomType != null)
                {
                    var roomTypeId = roomTypeService.GetRoomTypeInfoByName(roomType).Id;
                    var roomRTId = roomService.GetRoomById(r).RoomTypeId;
                    if (roomTypeId == roomRTId) return Json(new { success = true });
                }
                var wtData = new List<string>();
                foreach (var wt in waitingListService.GetAllWaitingList())
                {
                    wtData.Add(wt.RoomId);
                }

                // Get the current date
                DateTime currentDate = DateTime.Now;

                // Get the first day of the current month 
                DateTime firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);

                // Get the last day of the current month 
                DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                // Get Data
                var room = roomService.GetRoomById(r);
                var crrRoomType = roomTypeService.GetRoomTypeById(room.RoomTypeId);
                var allRoomList = roomService.GetAllRoomId(crrRoomType.Id).Except(wtData).ToList(); // Get the all room except the delete version 
                var allRoomListFtr = allRoomList.Except(roomId).ToList(); // Get the all room except the request room id and delete status 
                Console.WriteLine("COunt  > " + allRoomListFtr.Count);
                if (allRoomListFtr.Count == 0) return Json(new { message = "Your actions are rejected since have booking on schedule.", success = false });

                var reservations = reservationService.GetAllReservation().Where(rs => rs.CheckInDate >= firstDayOfMonth && rs.CheckInDate > currentDate &&
                    rs.CheckOutDate <= lastDayOfMonth && allRoomList.Contains(rs.RoomId)).GroupBy(rs => rs.RoomId).Select(g => g.First()).ToList();
                var reservationByRequest = reservationService.GetAllReservation().Where(rs => rs.CheckInDate >= currentDate.AddDays(-1) && rs.RoomId == room.Id).GroupBy(rs => rs.RoomId).Select(g => g.First()).ToList();
                Console.WriteLine("Total reservation : " + reservationByRequest.Count);
                var existList = new List<string>();
                var a = "";
                foreach (var rs in reservations)
                {
                    existList.Add(rs.RoomId);
                    a += rs.RoomId + ", ";
                }
                ///////////////////////////////////////////////////////

                // Future got reservation
                //var dupReserve = reservationService.GetAllReservation().Where(rs => rs.CheckInDate > currentDate && rs.CheckOutDate > currentDate
                //&& rs.RoomId == r).ToList();

                var randomRoomId = roomService.GetRandomRoomId(room.Id, reservationService.GetAllReservation(), waitingListService.GetAllWaitingList(), roomId);

                // Exist List = reservation data in the current month with the user request id
                // For future it will crushing or not
                // 12 / 30
                // Reassigned data during the data are in the reservation table when disabled or replace  both situation need to reassigned reservation room id

                if (allRoomListFtr.Count > 0 && reservationByRequest.Count > 0)
                {
                    // Check for future
                    var avg = (existList.Count / allRoomListFtr.Count) * 100;
                    if (randomRoomId == null) avg = 76;
                    if (avg > 75)
                    {
                        // Store into db waiting list
                        // Change the reservation future and store a time to delete 
                        err += createWaitingList(room, roomType, spec);

                        // Set the status to waiting for prevent the user confuse of the quantity
                        if (spec == null)
                        {
                            room.Status = "Waiting";
                            roomService.UpdateRoom(room);

                            crrRoomType.Quantity -= 1;
                            roomTypeService.UpdateRooomType(crrRoomType);
                        }
                    }
                    else
                    {
                        //Implement remove directly
                        if (roomType != null) // Replace part
                        {
                            if (randomRoomId != null)
                            {
                                msg = reservationService.ReAssigned(r, randomRoomId, allRoomList, reservationService.GetAllReservation());
                                if (msg != null)
                                {
                                    err += createWaitingList(room, roomType, spec);
                                    // Set the status to waiting for prevent the user confuse of the quantity
                                    if (spec == null)
                                    {
                                        room.Status = "Waiting";
                                        roomService.UpdateRoom(room);

                                        crrRoomType.Quantity -= 1;
                                        roomTypeService.UpdateRooomType(crrRoomType);
                                    }
                                }
                                else
                                {
                                    var newRoomType = roomTypeService.GetRoomTypeInfoByName(roomType);
                                    if (newRoomType == null) return Json(new { message = "No records founds.", success = false });
                                    if (spec == null)
                                    {
                                        room.RoomTypeId = newRoomType.Id;
                                        roomService.UpdateRoom(room);
                                        crrRoomType.Quantity -= 1;
                                        newRoomType.Quantity += 1;
                                        roomTypeService.UpdateRooomType(newRoomType);
                                        roomTypeService.UpdateRooomType(crrRoomType);
                                    }
                                }

                            }
                            else
                            {
                                err += createWaitingList(room, roomType, spec);
                                // Set the status to waiting for prevent the user confuse of the quantity
                                if (spec == null)
                                {
                                    room.Status = "Waiting";
                                    roomService.UpdateRoom(room);

                                    crrRoomType.Quantity -= 1;
                                    roomTypeService.UpdateRooomType(crrRoomType);
                                }
                            }
                        }
                        else
                        {
                            if (randomRoomId != null)
                            {
                                //return Json(new { message = $"{randomRoomId}", success = false });
                                msg = reservationService.ReAssigned(room.Id, randomRoomId, allRoomList, reservationService.GetAllReservation()); // Reassigned for future
                                if (msg != null)
                                {
                                    err += createWaitingList(room, roomType, spec);
                                    // Set the status to waiting for prevent the user confuse of the quantity
                                    if (spec == null)
                                    {
                                        room.Status = "Waiting";
                                        roomService.UpdateRoom(room);

                                        crrRoomType.Quantity -= 1;
                                        roomTypeService.UpdateRooomType(crrRoomType);
                                    }
                                }
                                else
                                {
                                    if (spec == null)
                                    {
                                        room.Status = "Disabled";
                                        roomService.UpdateRoom(room);
                                    }
                                }
                            }
                            else
                            {
                                // Store into db
                                err += createWaitingList(room, roomType, spec);
                                // Set the status to waiting for prevent the user confuse of the quantity
                                if (spec == null)
                                {
                                    room.Status = "Waiting";
                                    roomService.UpdateRoom(room);

                                    crrRoomType.Quantity -= 1;
                                    roomTypeService.UpdateRooomType(crrRoomType);
                                }
                            }
                        }
                    }
                }
                else if (reservationByRequest.Count == 0)
                {
                    if (roomType != null) // Replace part
                    {
                        var newRoomType = roomTypeService.GetRoomTypeInfoByName(roomType);
                        if (newRoomType == null) return Json(new { message = "No records founds.", success = false });
                        if (spec == null)
                        {
                            room.RoomTypeId = newRoomType.Id;
                            roomService.UpdateRoom(room);
                            crrRoomType.Quantity -= 1;
                            newRoomType.Quantity += 1;
                            roomTypeService.UpdateRooomType(newRoomType);
                            roomTypeService.UpdateRooomType(crrRoomType);
                        }
                    }
                    else
                    {
                        // Direct remove action
                        if (spec == null)
                        {
                            room.Status = "Disabled";
                            roomService.UpdateRoom(room);
                        }
                    }
                }
            }

            //return Json(new { message = "Not action activate.", success = false });
            if (err != "") return Json(new { message = $"{err}", success = false });
            return Json(new { message = $"{string.Join(", ", roomId)} is update successful.", success = true });
        }

        private string createWaitingList(Rooms room, string? roomType = null, string? spec = null)
        {
            var action = "";
            if (roomType == null) action = "Disabled";
            else action = "Replace";

            var newRoomType = "";
            if (roomType != null)
                newRoomType = roomTypeService.GetRoomTypeInfoByName(roomType).Id;

            var oldRoomType = room.RoomTypeId;


            var lastCheckOutDate = reservationService.GetAllReservation()
                                                        .Where(rs => rs.RoomId == room.Id)  // Filter by RoomId
                                                        .Select(g => g.CheckOutDate)  // Select the CheckOutDate
                                                        .OrderByDescending(c => c)   // Order by CheckOutDate in descending order
                                                        .FirstOrDefault();           // Get the first (most recent) CheckOutDate, or default if no reservation found
            if (spec == null)
            {
                waitingListService.AddWaitingList(new WaitingList
                {
                    RoomId = room.Id,
                    OrgRoomTypeId = oldRoomType,
                    Action = action,
                    NewRoomTypeId = newRoomType,
                    DatePerform = lastCheckOutDate,
                });
            }
            return $"Room {room.Name} will be action after {lastCheckOutDate} since not more room can be arranged. \n\n";
        }

        public void CheckWaitingList()
        {

        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///Custoerm Side
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [AllowAnonymous]
        [Route("Rooms")]
        public IActionResult Rooms(string checkInDate, string checkOutDate)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "Manager" || userRole == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
            }
            System.Diagnostics.Debug.WriteLine($"Rooms Method Invoked - CheckInDate: {checkInDate}, CheckOutDate: {checkOutDate}");

            List<RTWithImgVM> filteredRoomTypes;

            if (!string.IsNullOrEmpty(checkInDate) && !string.IsNullOrEmpty(checkOutDate))
            {
                try
                {
                    // Parse dates
                    DateTime checkIn = DateTime.Parse(checkInDate);
                    DateTime checkOut = DateTime.Parse(checkOutDate);

                    System.Diagnostics.Debug.WriteLine($"Parsed Dates - CheckIn: {checkIn}, CheckOut: {checkOut}");

                    // Ensure valid date range
                    if (checkOut <= checkIn)
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid Date Range - CheckOut: {checkOut}, CheckIn: {checkIn}");
                        return Json(new { success = false, message = "Check-Out date must be later than Check-In date." });
                    }

                    // Call the filtering logic
                    filteredRoomTypes = GetAvailableRoomTypes(checkIn, checkOut);

                    System.Diagnostics.Debug.WriteLine($"Filtered Room Types Count: {filteredRoomTypes.Count}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception Occurred: {ex.Message}");
                    return Json(new { success = false, message = "Invalid date format." });
                }
            }
            else
            {
                // Load all room types for initial page load
                //filteredRoomTypes = roomTypeService.GetAllRoomType().Where(rt => rt.Status == "Available").Take(6).ToList();
                filteredRoomTypes = roomTypeService.GetAllRoomType()
       .Where(rt => rt.Status == "Available")
       .Take(6)
       .Select(rt => new RTWithImgVM
       {
           RtId = rt.Id,
           RtName = rt.Name,
           Price = rt.Price,
           Images = db.RoomTypeImages
               .Where(img => img.RoomTypeId == rt.Id)
               .Select(img => img.Name) // Assuming ImageUrl is the property for the image path
               .FirstOrDefault() // Get the first image if available
       })
       .ToList();
                System.Diagnostics.Debug.WriteLine($"Initial Load - Total Room Types: {filteredRoomTypes.Count}");
            }

            // Return partial view for AJAX
            if (Request.IsAjax())
            {
                System.Diagnostics.Debug.WriteLine("AJAX Request Detected - Returning Partial View");
                return PartialView("_RoomCardPartial", filteredRoomTypes);
            }

            System.Diagnostics.Debug.WriteLine("Full Page Load - Returning Main View");
            return View("Rooms", filteredRoomTypes);
        }

        //[HttpGet("StoreTempData")]
        //public IActionResult StoreTempData(string roomTypeId)
        //{
        //    // Store the selected roomId in TempData
        //    TempData["SelectedRoomTypeId"] = roomTypeId;
        //    // Return a simple JSON response indicating success
        //    return Json(new { success = true });
        //}

        [AllowAnonymous]
        [HttpPost]
        public IActionResult StoreRoomTypeId(string id)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "Manager" || userRole == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
            }
            // Store the ID in the session
            HttpContext.Session.SetString("SelectedRoomTypeId", id);

            // Redirect to a generic path (e.g., Details page)
            var redirectUrl = Url.Action("RoomTypeDetails", "RoomType");
            return Json(new { redirectUrl });
        }

        public IActionResult RoomTypeDetails(int starRating = 0)
        {
            Console.WriteLine("Normal");
            // Retrieve the RoomTypeId from the session
            string? roomTypeId = HttpContext.Session.GetString("SelectedRoomTypeId");

            if (string.IsNullOrEmpty(roomTypeId))
            {
                return RedirectToAction("Rooms");
            }

            // Fetch the RoomType details
            var roomType = roomTypeService.GetRoomTypeById(roomTypeId);
            if (roomType == null)
            {
                return RedirectToAction("Rooms");
            }

            // Fetch all available rooms under the selected RoomType
            var availableRooms = roomService.GetAllRoomByCategoryId(roomTypeId);

            if (!availableRooms.Any())
            {
                System.Diagnostics.Debug.WriteLine($"RoomTypeDetails: No available rooms found for RoomTypeId '{roomTypeId}'.");
                ModelState.AddModelError("", "No rooms available under this room type.");
                return RedirectToAction("Rooms");
            }

            System.Diagnostics.Debug.WriteLine($"RoomTypeDetails: Found {availableRooms.Count} available rooms for RoomTypeId '{roomTypeId}'.");

            var allFeedback = feedbackService.GetAllFeedbacks();
            if (starRating != 0)
            {
                //allFeedback = feedbackService.GetAllFeedbacks().Where(fb => star == (double)Math.Ceiling(fb.Rating));
                allFeedback = feedbackService.GetAllFeedbacks()
    .Where(fb => starRating == (int)Math.Ceiling(fb.Rating))  // Compare with rounded-up rating
    .ToList();
            }
            var feedbackReservationList = new List<string>();

            foreach (var f in allFeedback)
            {
                feedbackReservationList.Add(f.ReservationId);
            }
            var roomTypeRoomList = roomService.GetAllRoomByCategoryId(roomTypeId);
            var reservationListWithRoomType = reservationService.GetAllReservation()
    .Where(rs => roomTypeRoomList.Any(room => room.Id == rs.RoomId)) // Filter by room type
    .Where(rs => feedbackReservationList.Contains(rs.Id)) // Filter by reservations that have feedback
    .ToList();

            var feedbacksInRoomType = allFeedback.Where(feedback =>
    reservationListWithRoomType.Any(reservation => reservation.Id == feedback.ReservationId)
).ToList();
            var allUsers = userService.GetAllUser();  // Get all users (or filter if necessary)

            // Join feedbacks with users to get the user name based on the userId
            var feedbacksWithUserNames = feedbacksInRoomType.Select(feedback =>
            {
                var user = allUsers.FirstOrDefault(u => u.Id == feedback.UserId);
                var images = feedbackMediaService.GetAllFeedbackMediaByFeedbackId(feedback.Id);
                var roomName = roomService.GetRoomById(reservationService.getReservationById(feedbackService.GetFeedbackById(feedback.Id).ReservationId).RoomId).Name;
                return new FeedbackWithUser
                {
                    Feedback = feedback,
                    User = user,  // Use "Unknown" if user is not found
                    Images = images,
                    RoomName = roomName,
                };
            }).ToList();

            var totalFeedbacks = feedbacksInRoomType.Count(); // Total number of feedbacks
            var ratingRanges = new List<(double Min, double Max, double GroupedRating)>
{
    (0.5, 1, 1),
    (1.5, 2, 2),
    (2.5, 3, 3),
    (3.5, 4, 4),
    (4.5, 5, 5)
};
            // Group feedbacks by their rating value
            var ratingCounts = ratingRanges.Select(range =>
            {
                var countInRange = feedbacksInRoomType.Count(fb => fb.Rating >= range.Min && fb.Rating <= range.Max); // Count feedbacks in the given range
                var percentage = totalFeedbacks > 0 ? (countInRange / (double)totalFeedbacks) * 100 : 0; // Calculate percentage
                return new { Rating = range.GroupedRating, Count = countInRange, Percentage = $"{percentage:F2}%" }; // Store percentage as a string
            }).ToList();

            // Store the result in a list
            var ratingPercentageList = ratingCounts.Select(r => new RatingPercentage
            {
                Rating = r.Rating,
                Count = r.Count,
                Percentage = r.Percentage // Store as string (e.g., "55.00%")
            }).OrderByDescending(r => r.Rating).ToList();

            double avgRate = feedbacksInRoomType.Any()
    ? Math.Round(feedbacksInRoomType.Average(fb => fb.Rating), 2)
    : 0; // Default to 0 if there are no feedbacks



            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = userService.GetUserByEmail(userEmail);

            // Populate the CreateReservationViewModel
            var viewModel = new CreateReservationViewModel
            {
                RatingPercentages = ratingPercentageList,
                FeedbackTotal = totalFeedbacks,
                AvgRate = avgRate,
                Feedback = feedbacksWithUserNames,
                RoomTypeImages = roomTypeImageService.GetRoomTypeImagesById(roomTypeId),
                RoomTypeId = roomTypeId,
                RoomType = roomType, // Pass the full RoomType details
                Room = availableRooms.FirstOrDefault(), // Automatically assign the first available room
                RoomList = availableRooms, // Pass all available rooms for reference
                RoomId = availableRooms.FirstOrDefault()?.Id, // Set the selected room ID
                RoomPrice = roomType.Price, // Use price from RoomType
                Description = roomType.Description,
                CapacityOptions = Enumerable.Range(1, roomType.Capacity).ToList(),
                UserName = user?.Name, // Set the user's name
                UserEmail = user?.Email // Set the user's email
            };


            //if (page < 1) // Ensure page number is valid
            //{
            //    return RedirectToAction(null, new { page = 1, pageSize});
            //}

            //var m = feedbacksWithUserNames.ToPagedList(page, pageSize); // Set page size = 5

            //if (page > m.PageCount && m.PageCount > 0) // Redirect if page exceeds total pages
            //{
            //    return RedirectToAction(null, new { page = m.PageCount, pageSize});
            //}
            Console.WriteLine("start >" + starRating);
            if (Request.IsAjax())
            {
                // Only pass the paged feedback list for the partial view
                Console.WriteLine("ajax!!!!");
                //return PartialView("_FeedbackList", feedbacksWithUserNames);
                Console.WriteLine("Count> " + feedbacksWithUserNames.Count);
                return PartialView("_FeedbackList", feedbacksWithUserNames);
            }

            Console.WriteLine("hereasddsadsa");
            System.Diagnostics.Debug.WriteLine($"RoomTypeDetails: ViewModel RoomId: {viewModel.RoomId}, RoomPrice: {viewModel.RoomPrice}");

            // Pass the view model to the view
            return View("RoomTypeDetails", viewModel);
        }

        //private List<RTWithImgVM> GetAvailableRoomTypes(DateTime checkIn, DateTime checkOut)
        //{
        //    System.Diagnostics.Debug.WriteLine($"GetAvailableRoomTypes Invoked - CheckIn: {checkIn}, CheckOut: {checkOut}");

        //    var occupiedRoomIds = db.Reservation
        //        .Where(res =>
        //            res.Status == "Pending" &&
        //            res.CheckInDate < checkOut &&
        //            res.CheckOutDate > checkIn)
        //        .Select(res => res.RoomId)
        //        .ToList();
        //    var wtList = waitingListService.GetAllWaitingList().Select(r => r.RoomId).ToList();  
        //    System.Diagnostics.Debug.WriteLine($"Occupied Room IDs Count: {occupiedRoomIds.Count}");

        //    var availableRoomTypes = db.Rooms
        //        .Where(r => !occupiedRoomIds.Contains(r.Id) && !wtList.Contains(r.Id))
        //        .Select(r => r.RoomType)
        //        .Distinct()
        //        .ToList();

        //    System.Diagnostics.Debug.WriteLine($"Available Room Types Count: {availableRoomTypes.Count}");

        //    return availableRoomTypes;
        //}
        private List<RTWithImgVM> GetAvailableRoomTypes(DateTime checkIn, DateTime checkOut)
        {
            System.Diagnostics.Debug.WriteLine($"GetAvailableRoomTypes Invoked - CheckIn: {checkIn}, CheckOut: {checkOut}");

            var occupiedRoomIds = db.Reservation
                .Where(res =>
                    res.Status == "Pending" &&
                    res.CheckInDate < checkOut &&
                    res.CheckOutDate > checkIn)
                .Select(res => res.RoomId)
                .ToList();
            var wtList = waitingListService.GetAllWaitingList().Select(r => r.RoomId).ToList();
            System.Diagnostics.Debug.WriteLine($"Occupied Room IDs Count: {occupiedRoomIds.Count}");

            // Get available room types
            var availableRoomTypes = db.Rooms
                .Where(r => !occupiedRoomIds.Contains(r.Id) && !wtList.Contains(r.Id))
                .Select(r => r.RoomType)
                .Distinct()
                .ToList();

            // Get the room type images (assuming ImageUrl is the name of the image property)
            var roomTypeImages = db.RoomTypeImages
                .GroupBy(img => img.RoomTypeId)
                .Select(g => new { RoomTypeId = g.Key, FirstImage = g.Select(img => img.Name).FirstOrDefault() })
                .ToList();

            // Map available room types with their first image
            var availableRoomTypesWithImages = availableRoomTypes
                .Select(rt => new RTWithImgVM
                {
                    RtId = rt.Id,
                    RtName = rt.Name,
                    Price = rt.Price,
                    // Get the first image for the room type
                    Images = roomTypeImages.FirstOrDefault(img => img.RoomTypeId == rt.Id)?.FirstImage
                })
                .ToList();

            System.Diagnostics.Debug.WriteLine($"Available Room Types Count: {availableRoomTypesWithImages.Count}");

            return availableRoomTypesWithImages;
        }


        [HttpPost]
        public IActionResult UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            if (request == null || request.Id == null || string.IsNullOrEmpty(request.Status))
            {
                return Json(new { success = false, message = "Invalid request." });
            }

            // Find the room type by ID (you can use your service/repository layer here)
            var roomType = roomTypeService.GetRoomTypeById(request.Id);
            if (roomType == null)
            {
                return Json(new { success = false, message = "Room type not found." });
            }

            // Update the status
            roomType.Status = request.Status;
            roomTypeService.UpdateRooomType(roomType); // Save changes to the database

            return Json(new { success = true });
        }

        //        [HttpGet("RoomTypeDetails")]
        //        //[Route("RoomTypeDetai")]
        //        public IActionResult RoomTypeDetail(int rate = 0)
        //        {
        //            Console.WriteLine("getmethods");
        //            // Retrieve the RoomTypeId from the session
        //            string? roomTypeId = HttpContext.Session.GetString("SelectedRoomTypeId");

        //            if (string.IsNullOrEmpty(roomTypeId))
        //            {
        //                return RedirectToAction("Rooms");
        //            }

        //            // Fetch the RoomType details
        //            var roomType = roomTypeService.GetRoomTypeById(roomTypeId);
        //            if (roomType == null)
        //            {
        //                return RedirectToAction("Rooms");
        //            }

        //            // Fetch all available rooms under the selected RoomType
        //            var availableRooms = roomService.GetAllRoomByCategoryId(roomTypeId);

        //            if (!availableRooms.Any())
        //            {
        //                System.Diagnostics.Debug.WriteLine($"RoomTypeDetails: No available rooms found for RoomTypeId '{roomTypeId}'.");
        //                ModelState.AddModelError("", "No rooms available under this room type.");
        //                return RedirectToAction("Rooms");
        //            }

        //            System.Diagnostics.Debug.WriteLine($"RoomTypeDetails: Found {availableRooms.Count} available rooms for RoomTypeId '{roomTypeId}'.");

        //            var allFeedback = feedbackService.GetAllFeedbacks();
        //            if (rate != 0)
        //            {
        //                //allFeedback = feedbackService.GetAllFeedbacks().Where(fb => star == (double)Math.Ceiling(fb.Rating));
        //                allFeedback = feedbackService.GetAllFeedbacks()
        //    .Where(fb => rate == (int)Math.Ceiling(fb.Rating))  // Compare with rounded-up rating
        //    .ToList();
        //            }
        //            var feedbackReservationList = new List<string>();

        //            foreach (var f in allFeedback)
        //            {
        //                feedbackReservationList.Add(f.ReservationId);
        //            }
        //            var roomTypeRoomList = roomService.GetAllRoomByCategoryId(roomTypeId);
        //            var reservationListWithRoomType = reservationService.GetAllReservation()
        //    .Where(rs => roomTypeRoomList.Any(room => room.Id == rs.RoomId)) // Filter by room type
        //    .Where(rs => feedbackReservationList.Contains(rs.Id)) // Filter by reservations that have feedback
        //    .ToList();

        //            var feedbacksInRoomType = allFeedback.Where(feedback =>
        //    reservationListWithRoomType.Any(reservation => reservation.Id == feedback.ReservationId)
        //).ToList();
        //            var allUsers = userService.GetAllUser();  // Get all users (or filter if necessary)

        //            // Join feedbacks with users to get the user name based on the userId
        //            var feedbacksWithUserNames = feedbacksInRoomType.Select(feedback =>
        //            {
        //                var user = allUsers.FirstOrDefault(u => u.Id == feedback.UserId);
        //                var images = feedbackMediaService.GetAllFeedbackMediaByFeedbackId(feedback.Id);
        //                var roomName = roomService.GetRoomById(reservationService.getReservationById(feedbackService.GetFeedbackById(feedback.Id).ReservationId).RoomId).Name;
        //                return new FeedbackWithUser
        //                {
        //                    Feedback = feedback,
        //                    User = user,  // Use "Unknown" if user is not found
        //                    Images = images,
        //                    RoomName = roomName,
        //                };
        //            }).ToList();

        //            var totalFeedbacks = feedbacksInRoomType.Count(); // Total number of feedbacks
        //            var ratingRanges = new List<(double Min, double Max, double GroupedRating)>
        //        {
        //            (0.5, 1, 1),
        //            (1.5, 2, 2),
        //            (2.5, 3, 3),
        //            (3.5, 4, 4),
        //            (4.5, 5, 5)
        //        };
        //            // Group feedbacks by their rating value
        //            var ratingCounts = ratingRanges.Select(range =>
        //            {
        //                var countInRange = feedbacksInRoomType.Count(fb => fb.Rating >= range.Min && fb.Rating <= range.Max); // Count feedbacks in the given range
        //                var percentage = totalFeedbacks > 0 ? (countInRange / (double)totalFeedbacks) * 100 : 0; // Calculate percentage
        //                return new { Rating = range.GroupedRating, Count = countInRange, Percentage = $"{percentage:F2}%" }; // Store percentage as a string
        //            }).ToList();

        //            // Store the result in a list
        //            var ratingPercentageList = ratingCounts.Select(r => new RatingPercentage
        //            {
        //                Rating = r.Rating,
        //                Count = r.Count,
        //                Percentage = r.Percentage // Store as string (e.g., "55.00%")
        //            }).ToList();

        //            double avgRate = feedbacksInRoomType.Average(fb => fb.Rating);

        //            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
        //            var user = userService.GetUserByEmail(userEmail);

        //            // Populate the CreateReservationViewModel
        //            var viewModel = new CreateReservationViewModel
        //            {
        //                RatingPercentages = ratingPercentageList,
        //                FeedbackTotal = totalFeedbacks,
        //                AvgRate = avgRate,
        //                Feedback = feedbacksWithUserNames,
        //                RoomTypeId = roomTypeId,
        //                RoomType = roomType, // Pass the full RoomType details
        //                Room = availableRooms.FirstOrDefault(), // Automatically assign the first available room
        //                RoomList = availableRooms, // Pass all available rooms for reference
        //                RoomId = availableRooms.FirstOrDefault()?.Id, // Set the selected room ID
        //                RoomPrice = roomType.Price, // Use price from RoomType
        //                Description = roomType.Description,
        //                CapacityOptions = Enumerable.Range(1, roomType.Capacity).ToList(),
        //                UserName = user?.Name, // Set the user's name
        //                UserEmail = user?.Email // Set the user's email
        //            };

        //            if (Request.IsAjax())
        //            {
        //                // Only pass the paged feedback list for the partial view
        //                Console.WriteLine("ajax!!!!");
        //                return PartialView("_FeedbackList", feedbacksWithUserNames);
        //            }
        //            Console.WriteLine("herecxxvx");
        //            System.Diagnostics.Debug.WriteLine($"RoomTypeDetails: ViewModel RoomId: {viewModel.RoomId}, RoomPrice: {viewModel.RoomPrice}");

        //            // Pass the view model to the view
        //            return View("RoomTypeDetails", viewModel);
        //        }


        //    [HttpPost]
        //    [Route("RoomTypeDetails")]
        //    public IActionResult RoomTypeDetail(int star = 0)
        //    {
        //        // Retrieve the RoomTypeId from the session
        //        string? roomTypeId = HttpContext.Session.GetString("SelectedRoomTypeId");

        //        if (string.IsNullOrEmpty(roomTypeId))
        //        {
        //            return RedirectToAction("Rooms");
        //        }

        //        // Fetch RoomType details
        //        var roomType = roomTypeService.GetRoomTypeById(roomTypeId);
        //        if (roomType == null)
        //        {
        //            return RedirectToAction("Rooms");
        //        }

        //        // Fetch available rooms under the selected RoomType
        //        var availableRooms = roomService.GetAllRoomByCategoryId(roomTypeId);

        //        if (!availableRooms.Any())
        //        {
        //            ModelState.AddModelError("", "No rooms available under this room type.");
        //            return RedirectToAction("Rooms");
        //        }

        //        // Fetch all feedback
        //        var allFeedback = feedbackService.GetAllFeedbacks();
        //        if (star > 0)
        //        {
        //            allFeedback = allFeedback
        //                .Where(fb => (int)Math.Ceiling(fb.Rating) == star) // Filter feedback by star rating
        //                .ToList();
        //        }

        //        // Filter feedback by RoomType and reservations
        //        var feedbackReservationList = allFeedback.Select(f => f.ReservationId).ToList();
        //        var roomTypeRoomList = roomService.GetAllRoomByCategoryId(roomTypeId);
        //        var reservationListWithRoomType = reservationService.GetAllReservation()
        //            .Where(rs => roomTypeRoomList.Any(room => room.Id == rs.RoomId)) // Filter by room type
        //            .Where(rs => feedbackReservationList.Contains(rs.Id)) // Filter by feedback
        //            .ToList();

        //        var feedbacksInRoomType = allFeedback
        //            .Where(feedback => reservationListWithRoomType.Any(reservation => reservation.Id == feedback.ReservationId))
        //            .ToList();

        //        // Join feedbacks with users to get usernames
        //        var allUsers = userService.GetAllUser();
        //        var feedbacksWithUserNames = feedbacksInRoomType.Select(feedback =>
        //        {
        //            var user = allUsers.FirstOrDefault(u => u.Id == feedback.UserId);
        //            var images = feedbackMediaService.GetAllFeedbackMediaByFeedbackId(feedback.Id);
        //            var roomName = roomService.GetRoomById(
        //                reservationService.getReservationById(feedback.ReservationId).RoomId).Name;
        //            return new FeedbackWithUser
        //            {
        //                Feedback = feedback,
        //                User = user,
        //                Images = images,
        //                RoomName = roomName
        //            };
        //        }).ToList();

        //        // Calculate feedback statistics
        //        var totalFeedbacks = feedbacksInRoomType.Count();
        //        double avgRate = feedbacksInRoomType.Any() ? feedbacksInRoomType.Average(fb => fb.Rating) : 0;
        //        var ratingRanges = new List<(double Min, double Max, double GroupedRating)>
        //{
        //    (0.5, 1, 1),
        //    (1.5, 2, 2),
        //    (2.5, 3, 3),
        //    (3.5, 4, 4),
        //    (4.5, 5, 5)
        //};

        //        var ratingCounts = ratingRanges.Select(range =>
        //        {
        //            var countInRange = feedbacksInRoomType.Count(fb => fb.Rating >= range.Min && fb.Rating <= range.Max);
        //            var percentage = totalFeedbacks > 0 ? (countInRange / (double)totalFeedbacks) * 100 : 0;
        //            return new { Rating = range.GroupedRating, Count = countInRange, Percentage = $"{percentage:F2}%" };
        //        }).ToList();

        //        var ratingPercentageList = ratingCounts.Select(r => new RatingPercentage
        //        {
        //            Rating = r.Rating,
        //            Count = r.Count,
        //            Percentage = r.Percentage
        //        }).ToList();

        //        // Get user details
        //        var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
        //        var user = userService.GetUserByEmail(userEmail);

        //        // Create ViewModel
        //        var viewModel = new CreateReservationViewModel
        //        {
        //            RatingPercentages = ratingPercentageList,
        //            FeedbackTotal = totalFeedbacks,
        //            AvgRate = avgRate,
        //            Feedback = feedbacksWithUserNames,
        //            RoomTypeId = roomTypeId,
        //            RoomType = roomType,
        //            Room = availableRooms.FirstOrDefault(),
        //            RoomList = availableRooms,
        //            RoomId = availableRooms.FirstOrDefault()?.Id,
        //            RoomPrice = roomType.Price,
        //            Description = roomType.Description,
        //            CapacityOptions = Enumerable.Range(1, roomType.Capacity).ToList(),
        //            UserName = user?.Name,
        //            UserEmail = user?.Email
        //        };

        //        // Handle AJAX requests
        //        if (Request.IsAjax())
        //        {
        //            Console.WriteLine("jkhasjkd");
        //            return PartialView("_FeedbackList", feedbacksWithUserNames);
        //        }

        //        Console.WriteLine("out");


        //        // Return full view for non-AJAX requests
        //        return View("RoomTypeDetails", viewModel);
        //    }

    }
}
