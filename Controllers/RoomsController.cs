using HotelRoomReservationSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using HotelRoomReservationSystem.BLL.Interfaces;
using X.PagedList.Extensions;
using HotelRoomReservationSystem.DAL;
using System.Diagnostics.Eventing.Reader;
using HotelRoomReservationSystem.DAL.Interfaces;
using HotelRoomReservationSystem.BLL;
using DocumentFormat.OpenXml.Wordprocessing;
using Stripe.Tax;
using HotelRoomReservationSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelRoomReservationSystem.Controllers
{
    public class RoomsController : Controller
    {
        private readonly IRoomService roomService;
        private readonly IRoomTypeService categoryService;
        private readonly IRoomTypeImageService roomImageService;
        private readonly Helper hp;

        public RoomsController(IRoomService roomService, IRoomTypeService categoryService, IRoomTypeImageService roomImageService, Helper hp)
        {
            this.roomService = roomService;
            this.categoryService = categoryService;
            this.roomImageService = roomImageService;
            this.hp = hp;
        }

        [Route("RoomManagement")]
        public IActionResult Index(string? searchBar, string? sort, string? dir, int page = 1, int pageSize = 5, string status = "All", string roomType = "All")
        {
            ViewBag.Name = searchBar = searchBar?.Trim() ?? "";
            ViewBag.PageSize = pageSize;
            ViewBag.Page = page;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;
            ViewBag.RoomType = roomType;

            var rooms = string.IsNullOrEmpty(searchBar)
    ? roomService.GetAllRooms()
    : roomService.GetAllRoomsById(searchBar);

            var categories = categoryService.GetAllRoomType();

            var roomTypes = categories
    .Select(rt => new SelectListItem
    {
        Text = rt.Name,
        Value = rt.Id
    }).ToList();

            roomTypes.Insert(0, new SelectListItem { Text = "All", Value = "All" }); // Add "All" option
            ViewBag.RoomTypes = roomTypes;

            // Map the relationships
            var roomDetails = rooms.Select(room => new RoomDetailsVM
            {
                RoomId = room.Id,
                RoomName = room.Name,
                Status = room.Status,
                RoomType = categories.FirstOrDefault(c => c.Id == room.RoomTypeId),
            }).ToList();

            if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                roomDetails = roomDetails
                    .Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            //if (!string.Equals(roomType, "All", StringComparison.OrdinalIgnoreCase))
            //{
            //    var rtId = categoryService.GetRoomTypeInfoByName(roomType).Id;
            //    roomDetails = roomDetails.Where(r => string.Equals(r.RoomType.Id, rtId, StringComparison.OrdinalIgnoreCase)).ToList();
            //}
            if (!string.Equals(roomType, "All", StringComparison.OrdinalIgnoreCase))
            {
                var roomTypeInfo = categoryService.GetRoomTypeById(roomType);

                if (roomTypeInfo != null)
                {
                    var rtId = roomTypeInfo.Id;
                    roomDetails = roomDetails
                        .Where(r => string.Equals(r.RoomType?.Id, rtId, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    Console.WriteLine($"Invalid roomType: {roomType}");
                    return RedirectToAction("Index", new { error = "Invalid Room Type" });
                }
            }
        


            if (pageSize == 0) pageSize = roomService.GetAllRooms().Count;

            // Define sorting function based on the sort parameter
            Func<RoomDetailsVM, object> fn = sort switch
            {
                "No" => r => r.RoomId,
                "RoomName" => r => r.RoomName,
                "Status" => r => r.Status,
                "RoomType" => r => r.RoomType.Name,
                _ => r => r.RoomId // Default sorting column
            };

            var sorted = dir == "des" ? roomDetails.OrderByDescending(fn) : roomDetails.OrderBy(fn);

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
                return PartialView("_RoomList", m); // Return only the updated reservation list and pagination controls
            }

            return View("RoomManagement", m);
        }

        [HttpGet]
        [Route("Index")]
        public IActionResult RoomManagement(string? searchBar, string? sort, string? dir, int page = 1, int pageSize = 5, string status = "All")
        {
            ViewBag.Name = searchBar = searchBar?.Trim() ?? "";
            ViewBag.PageSize = pageSize;
            ViewBag.Page = page;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;
            var rooms = string.IsNullOrEmpty(searchBar)
    ? roomService.GetAllRooms()
    : roomService.GetAllRoomsById(searchBar);

            var categories = categoryService.GetAllRoomType();

            // Map the relationships
            var roomDetails = rooms.Select(room => new RoomDetailsVM
            {
                RoomId = room.Id,
                RoomName = room.Name,
                Status = room.Status,
                RoomType = categories.FirstOrDefault(c => c.Id == room.RoomTypeId),
            }).ToList();
            if (pageSize == 0) pageSize = roomService.GetAllRooms().Count;

            if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                roomDetails = roomDetails
                    .Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            dir = dir?.ToLower() == "des" ? "des" : "asc";

            // Define sorting function based on the sort parameter
            Func<RoomDetailsVM, object> fn = sort switch
            {
                "RoomId" => r => r.RoomId,
                "RoomName" => r => r.RoomName,
                "Status" => r => r.Status,
                "RoomTypeName" => r => r.RoomType.Name,
                _ => r => r.RoomId // Default sorting column
            };

            var sorted = dir == "des" ? roomDetails.OrderByDescending(fn) : roomDetails.OrderBy(fn);

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
                return PartialView("_RoomList", m); // Return only the updated reservation list and pagination controls
            }

            return View("RoomManagement", m);
        }

        public RoomDetailsVM GetRoomDetailsById(string roomId)
        {
            var room = roomService.GetRoomById(roomId);

            if (room == null)
            {
                // Handle the case where the room is not found
                return null;
            }

            // Fetch related data
            var roomImages = roomImageService.GetRoomTypeImagesById(roomId);
            var roomType = categoryService.GetRoomTypeById(room.RoomTypeId);

            // Map the single room object to the ViewModel
            var roomDetails = new RoomDetailsVM
            {
                RoomId = room.Id,
                RoomName = room.Name,
                RoomDescription = room.RoomType.Description,
                Status = room.Status,
                RoomType = roomType, // Assuming categoryService.GetCategoryById() returns a single roomType
                Images = roomImages.Where(img => img.RoomTypeId == roomType.Id).ToList()
            };

            // Return or use roomDetails as needed
            return roomDetails;
        }

        //[HttpGet]
        //public IActionResult GetRoomDetails(string roomId)
        //{
        //    var room = roomService.GetRoom(roomId);

        //    if (room == null)
        //    {
        //        return NotFound(new { message = "Room not found" });
        //    }

        //    // Map room details to a response object
        //    var response = new
        //    {
        //        id = room.Id,
        //        name = room.Name,
        //        description = room.Description,
        //        roomType = room.RoomType.Name,
        //        quantity = room.RoomType.Quantity,
        //        capacity = room.RoomType.Capacity,
        //        price = room.RoomType.Price,
        //        status = room.RoomType.Status,
        //        images = room.RoomImages.Select(img => Convert.ToBase64String(img.ImageData)).ToList()
        //    };

        //    return Json(response);
        //}

        [Route("RForm")]
        public IActionResult AddForm()
        {
            ViewBag.CategoryList = categoryService.GetRoomTypeNameList(); // Returns List<string
            ViewBag.Id = roomService.GenerateCode();
            return View("RForm");
        }

        public bool CheckIdAvailability(string id)
        {
            var existingRoom = roomService.GetRoomById(id);
            if (existingRoom != null)
            {
                return false;
            }
            return true;
        }

        [AcceptVerbs("GET", "POST")]
        public IActionResult CheckIdAvailable(string id)
        {
            var existingRoom = roomService.GetRoomById(id);
            if (existingRoom != null)
            {
                return Json($"Room Id '{id}' is already in use.");
            }
            return Json(true);
        }

        [AcceptVerbs("GET", "POST")]
        public IActionResult CheckNameAvailable(string name, string id)
        {
            var existingRoom = roomService.GetAllRooms()
                                              .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && r.Id != id);
            if (existingRoom != null)
            {
                return Json($"Room Name '{name}' is already in use.");
            }
            return Json(true);
        }

        [AcceptVerbs("GET", "POST")]
        public IActionResult CheckAddIdAvailable(string id)
        {
            var existingRoom = roomService.GetRoomById(id);
            if (existingRoom != null)
            {
                return Json($"Room Id '{id}' is already in use.");
            }
            return Json(true);
        }

        [AcceptVerbs("GET", "POST")]
        public IActionResult CheckAddNameAvailable(string name, string id)
        {
            var existingRoom = roomService.GetAllRooms()
                                              .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && r.Id != id);
            if (existingRoom != null)
            {
                return Json($"Room Name '{name}' is already in use.");
            }
            return Json(true);
        }


        public bool CheckNameAvailability(string name, string id)
        {
            var existingRoom = roomService.GetAllRooms()
                                              .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && r.Id != id);
            if (existingRoom != null)
            {
                bool isAvailable = roomService.IsNameAvailable(name);
                return isAvailable;
            }
            return true;
        }
        public void CheckImages(RoomsAddVM model)
        {
            var e = hp.ValidateFiles(model.Images);
            if (e != "")
            {
                ModelState.AddModelError("photo", e);
            }
            else
            {
                foreach (var image in model.Images)
                {
                    if (image.Length > 0)
                    {
                        // Define the directory to save the images
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/uploads");

                        // Ensure the directory exists
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        // Generate a unique file name
                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                        var filePath = Path.Combine(uploadPath, uniqueFileName);

                        // Save the image to the directory
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            image.CopyTo(fileStream);
                        }

                        // Add the relative path to the model for further use (e.g., display in the UI)
                        model.ImagePreviews.Add($"/images/uploads/{uniqueFileName}");
                    }
                    else
                    {
                        if (model.ImagePreviews.Count > 0)
                        {
                            //model.ImagePreviews.Add(model.ImagePreviews.Any();
                        }
                    }
                }

            }
        }

        public void UploadImages(RoomsAddVM model)
        {
            Console.WriteLine(model.RoomTypeName);
            foreach (var file in model.Images)
            {
                //var imageId = roomImageService.GenerateImageId(DateTime.Now);
                var imageName = roomImageService.AddImages(file, categoryService.GetRoomTypeInfoByName(model.RoomTypeName).Id);
                hp.SaveImage(file, "images/RoomType", imageName);
            }
        }

        public bool CheckNameAvailability(string name)
        {
            bool isAvailable = roomService.IsNameAvailable(name);
            return isAvailable;
        }

        [HttpPost]
        public IActionResult SubmitForm(RoomsAddVM vm, List<string> ExistingPreviews, string? id = null)
        {
            if (id != null)
                vm.Id = id;
            else
                vm.Id = roomService.GenerateCode();

            ModelState.Remove("Id");

            if (ModelState.IsValid("Name") && !CheckNameAvailability(vm.Name, vm.Id))
            {
                ModelState.AddModelError("Name", "Duplicated Name.");
            }

            var e = hp.ValidateFiles(vm.Images);
            if (e != "")
            {
                ModelState.AddModelError("Images", e);
            }

            // Ensure the existing previews are retained
            if (ExistingPreviews != null && ExistingPreviews.Count > 0 && vm.Images == null)
            {
                vm.ImagePreviews.AddRange(ExistingPreviews);
            }
            if (vm.Images != null)
            {
                foreach (var image in vm.Images)
                {
                    if (image.Length > 0)
                    {
                        // Define the directory to save the images
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/uploads");

                        // Ensure the directory exists
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        // Generate a unique file name
                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                        var filePath = Path.Combine(uploadPath, uniqueFileName);

                        // Save the image to the directory
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            image.CopyTo(fileStream);
                        }

                        // Add the relative path to the model for further use
                        vm.ImagePreviews.Add($"/images/uploads/{uniqueFileName}");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                if (CheckIdAvailability(vm.Id)) // Check whether is it update form or not, if that is update form it will go to else
                {
                    if (categoryService.CheckNameAvailability(vm.RoomTypeName)) // if not match means that using the existing category.
                    {
                        categoryService.AddRoomType(new RoomType
                        {
                            Id = categoryService.GenerateCode(),
                            Name = vm.RoomTypeName,
                            Description = vm.Description,
                            Price = vm.Price,
                            Capacity = vm.RoomCapacity,
                            Quantity = 1,
                            DateCreated = DateTime.Now,
                            Status = "Available",
                        });
                        Console.WriteLine("Id > " + vm.Id);
                        UploadImages(vm);
                    }

                    roomService.AddRoom(new Rooms
                    {
                        Id = vm.Id,
                        Name = vm.Name,
                        Status = "Available",
                        DateCreated = DateTime.Now,
                        RoomTypeId = categoryService.GetRoomTypeInfoByName(vm.RoomTypeName).Id,
                    });
                    return RedirectToAction("Index");
                }
                else
                {
                    var existRoom = roomService.GetRoomById(vm.Id);
                    if (existRoom != null)
                    {
                        if (categoryService.CheckNameAvailability(vm.RoomTypeName))
                        {
                            categoryService.AddRoomType(new RoomType
                            {
                                Id = categoryService.GenerateCode(),
                                Name = vm.RoomTypeName,
                                Description = vm.Description,
                                Price = vm.Price,
                                DateCreated = DateTime.Now,
                                Quantity = 1,
                                Status = "Available",
                            });

                            UploadImages(vm);
                        }

                        existRoom.Name = vm.Name;
                        existRoom.RoomTypeId = categoryService.GetRoomTypeInfoByName(vm.RoomTypeName).Id;
                        roomService.UpdateRoom(existRoom);

                        return RedirectToAction("Index");
                    }
                }
            }

            ViewBag.CategoryList = categoryService.GetRoomTypeNameList(); // Returns List<string
            return View("RForm", vm);
        }

        [HttpGet]
        public IActionResult GetCategoryDetails(string roomTypeName)
        {
            var roomType = categoryService.GetRoomTypeInfoByName(roomTypeName); // Fetch roomType details
            if (roomType == null)
            {
                return Json(null); // Return null if not found
            }
            var img = string.Join(", ", roomImageService.GetRoomTypeImagesNameById(roomType.Id));
            Console.WriteLine("Images  > " + img);
            Console.WriteLine("Id > " + roomType.Id);

            return Json(new
            {
                price = roomType.Price,
                description = roomType.Description,
                capacity = roomType.Capacity,
                images = img,
                roomTypeId = roomType.Id,
            });
        }

        // Admin 
        [HttpGet]
        [Route("Rooms/getRoomDetails")]
        public IActionResult GetRoomDetails(string roomId)
        {
            try
            {
                // Fetch the room details from your service or repository
                var room = roomService.GetRoomById(roomId);

                if (room == null)
                {
                    return NotFound(new { Message = "Room not found." });
                }

                // Return as JSON
                return Json(roomId);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine(ex);

                // Return a failure response
                return StatusCode(500, new { Message = "An error occurred while fetching room details." });
            }
        }

        // Admin
        [HttpGet]
        [Route("Rooms/Details")]
        public IActionResult Details(string roomId)
        {
            try
            {
                // Fetch the room details
                var room = roomService.GetRoomById(roomId);

                if (room == null)
                {
                    // Redirect to an error page or show a "not found" message
                    return RedirectToAction("Error", new { message = "Room not found." });
                }
                // Fetch related data
                var roomImages = roomImageService.GetRoomTypeImagesNameById(roomId);
                var roomType = categoryService.GetRoomTypeById(room.RoomTypeId);

                // Map the single room object to the ViewModel
                var roomDetails = new RoomsUpdateVM
                {
                    Id = room.Id,
                    Name = room.Name,
                    Description = roomType.Description,
                    Price = roomType.Price,
                    RoomCapacity = roomType.Capacity,
                    RoomTypeName = roomType.Name, // Assuming categoryService.GetCategoryById() returns a single roomType
                    ImagePreviews = roomImages,
                };

                ViewBag.CategoryList = categoryService.GetRoomTypeNameList(); // Returns List<string
                // Return the room details to the view
                return View("UpdateForm", roomDetails);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine(ex);

                // Redirect to an error page or show a "server error" message
                return RedirectToAction("Error", new { message = "An error occurred while loading the room details." });
            }
        }

        [HttpGet]
        [Route("RoomDelete")]
        public IActionResult Delete([FromQuery] string roomIds)
        {
            if (string.IsNullOrWhiteSpace(roomIds))
            {
                return BadRequest(new { message = "No rooms selected for deletion." });
            }

            var roomIdList = roomIds.Split(',').ToList();

            try
            {
                // Call service to delete rooms
                roomService.RemoveRoom(roomIdList);

                return Ok(new { message = $"{roomIdList.Count} room(s) deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting rooms.", details = ex.Message });
            }
        }

        public void commitData()
        {
            roomService.CommitTransaction();
        }

        public void cancelTransaction()
        {
            roomService.CommitTransaction();
        }

        [HttpPost]
        public IActionResult UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            if (request == null || request.Id == null || string.IsNullOrEmpty(request.Status))
            {
                return Json(new { success = false, message = "Invalid request." });
            }

            // Find the room type by ID (you can use your service/repository layer here)
            var room = roomService.GetRoomById(request.Id);
            if (room == null)
            {
                return Json(new { success = false, message = "Room type not found." });
            }

            // Update the status
            room.Status = request.Status;
            roomService.UpdateRoomStatus(room); // Save changes to the database

            return Json(new { success = true });
        }
    }
}
