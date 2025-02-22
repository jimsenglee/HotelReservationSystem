using HotelRoomReservationSystem.DAL;
using Microsoft.AspNetCore.Mvc;
using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.DAL.Interfaces;
using System.Text.RegularExpressions;
using PdfSharp.UniversalAccessibility;
using System.Linq;

namespace HotelRoomReservationSystem.BLL
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository roomRepository;
        public RoomService(IRoomRepository roomRepository)
        {
            this.roomRepository = roomRepository;
        }
        public List<Rooms> GetAllRooms()
        {
            return roomRepository.GetAllRooms();
        }

        public List<Rooms> GetAllRoomsById(string searchBar)
        {
            return roomRepository.GetRoomsById(searchBar);
        }
        public Rooms GetRoomByName(string roomName)
        {
            return roomRepository.GetDataByName(roomName);
        }
        public Rooms GetRoomById(string roomId)
        {
            return roomRepository.GetRoom(roomId);
        }
        public List<Rooms> GetAllRoomByCategoryId(string catgeoryId)
        {
            return roomRepository.GetDataByCtgId(catgeoryId);
        }



        public bool IsIdAvailable(string id)
        {
            return roomRepository.CheckId(id);
        }

        public bool IsNameAvailable(string name)
        {
            return roomRepository.CheckName(name);
        }

        public bool AddRoom(Rooms room)
        {
            int i = roomRepository.SaveRoom(room);
            if (i == 0)
                return true;
            return false;
        }

        //public void RemoveRoom(string roomId)
        //{
        //    roomRepository.Delete(roomId);
        //}

        //public void RemoveRoom(List<string> roomIds)
        //{
        //    try
        //    {
        //        // Start transaction
        //        roomRepository.BeginTransaction();

        //        // Perform room deletions
        //        foreach (var roomId in roomIds)
        //        {
        //            roomRepository.Delete(roomId);
        //        }

        //        // Save changes 
        //        roomRepository.SaveChanges();
        //    }
        //    catch
        //    {
        //        // Rollback transaction on error
        //        roomRepository.RollbackTransaction();
        //        throw;
        //    }
        //}

        //public void CommitTransaction()
        //{
        //    roomRepository.CommitTransaction();
        //} 
        //public void RollTransaction()
        //{
        //    roomRepository.RollbackTransaction();
        //}

        //public async void RemoveRoom(IEnumerable<string> roomIds)
        //{
        //    roomRepository.BeginTransaction(); // Start transaction

        //    try
        //    {
        //        foreach (var roomId in roomIds)
        //        {
        //            roomRepository.Delete(roomId);
        //        }

        //        roomRepository.SaveChanges(); // Save changes but do not commit
        //        await Task.Delay(5000);
        //        CommitTransaction();
        //    }
        //    catch
        //    {
        //        roomRepository.RollbackTransaction(); // Rollback if an error occurs
        //        throw;
        //    }
        //}

        public async void RemoveRoom(IEnumerable<string> roomIds)
        {
            roomRepository.BeginTransaction(); // Start transaction

            try
            {
                foreach (var roomId in roomIds)
                {
                    roomRepository.Delete(roomId); // Mark rooms for deletion
                }

                roomRepository.SaveChanges(); // Save changes but do not commit

                Console.WriteLine("Waiting for undo action (5 seconds)... Press 'U' to undo.");

                // Create a cancellation token to monitor undo action
                var cts = new CancellationTokenSource();
                var undoTask = WaitForUndoAsync(cts.Token);

                // Wait for either the undo action or a 5-second timeout
                var delayTask = Task.Delay(5000);

                if (await Task.WhenAny(undoTask, delayTask) == undoTask && undoTask.Result)
                {
                    // User clicked undo, rollback the transaction
                    roomRepository.RollbackTransaction();
                    Console.WriteLine("Transaction rolled back due to user undo.");
                }
                else
                {
                    // No undo action within the time frame, commit the transaction
                    roomRepository.CommitTransaction();
                    Console.WriteLine("Transaction committed successfully.");
                }
            }
            catch
            {
                roomRepository.RollbackTransaction(); // Rollback if an error occurs
                Console.WriteLine("Transaction rolled back due to an error.");
                throw;
            }
        }

        // Helper method to listen for "Undo" action asynchronously
        private static async Task<bool> WaitForUndoAsync(CancellationToken token)
        {
            return await Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true).Key;
                        if (key == ConsoleKey.U) // Press 'U' for undo
                        {
                            return true;
                        }
                    }
                    Thread.Sleep(100); // Check periodically
                }
                return false;
            });
        }

        public void CommitTransaction()
        {
            roomRepository.CommitTransaction();
        }

        public void RollTransaction()
        {
            roomRepository.RollbackTransaction();
        }

        public string GenerateCode()
        {
            string max = roomRepository.getLast() ?? "RM000";
            int n = int.Parse(max[2..]);
            return (n + 1).ToString("'RM'000");
        }

        public string CheckNameRange(string StartRoom, string EndRoom)
        {
            string startPart = Regex.Match(StartRoom, @"\d+").Value;
            string endPart = Regex.Match(EndRoom, @"\d+").Value;
            int start = int.Parse(startPart);
            int end = int.Parse(endPart);
            if (end < start)
            {
                return "The end room number should be bigger than the start room.";
            }

            //var roomsName = GetExistingRoom(StartRoom, EndRoom);
            //if (roomsName == null)
            //{
            //    return "No rooms found to validate.";
            //}
            //else if (roomsName != null)
            //{
            //    var nameList = string.Join(", ", roomsName);
            //    return $"In this range, the following rooms have duplicates: {nameList}.";
            //}

            return ""; // No duplicates found
        }

        public List<string> GetExistingRoom(string startRoom, string endRoom)
        {
            string startPart = Regex.Match(startRoom, @"\d+").Value;
            string endPart = Regex.Match(endRoom, @"\d+").Value;
            int start = int.Parse(startPart);
            int end = int.Parse(endPart);
            var roomsName = new List<string>();
            var allRooms = GetAllRooms();

            if (!allRooms.Any())
            {
                return null;
            }


            var startPrefix = startRoom.Substring(0, 1);  // Get the prefix (letter) from StartRoom
            var endPrefix = endRoom.Substring(0, 1);      // Get the prefix (letter) from EndRoom

            try
            {
                foreach (var room in allRooms)
                {
                    string roomNum = Regex.Match(room.Name, @"\d+").Value; // Extract numeric part of the room name

                    // Check if the room has a valid numeric part
                    if (!string.IsNullOrEmpty(roomNum) && int.TryParse(roomNum, out int num))
                    {
                        string roomPrefix = room.Name.Substring(0, 1); // Extract prefix from the room name

                        // Only consider rooms with matching prefixes (if applicable) and that fall within the numeric range
                        if (roomPrefix.Equals(startPrefix, StringComparison.OrdinalIgnoreCase) &&
                            num >= start && num <= end)
                        {
                            roomsName.Add(room.Name);
                        }
                    }
                }

                //If duplicates are found, return the formatted message
                if (roomsName.Any())
                {
                    return roomsName;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        public List<string> GetRange(string startRoom, string endRoom)
        {
            // Extract numeric parts from the room names
            string startPart = Regex.Match(startRoom, @"\d+").Value;
            string endPart = Regex.Match(endRoom, @"\d+").Value;

            // Parse the numeric parts to integers
            int start = int.Parse(startPart);
            int end = int.Parse(endPart);

            // Extract the prefix (letters) from StartRoom
            string startPrefix = Regex.Match(startRoom, @"^[A-Za-z]+").Value;

            var roomsName = new List<string>();

            // Generate the range of room names
            for (int i = start; i <= end; i++)
            {
                roomsName.Add($"{startPrefix}{i:D3}"); // Use string interpolation and format with leading zeros
            }

            return roomsName;
        }

        public bool UpdateRoomStatus(Rooms room)
        {
            if (room == null || room.Id == null)
                return false;

            // Delegate the actual update to the repository
            return roomRepository.Update(room);
        }

        public bool UpdateRoom(Rooms room)
        {
            return roomRepository.Update(room);
        }

        public List<string>? ConvertToRange(List<Rooms> roomList)
        {
            if (roomList == null || roomList.Count == 0) return null;

            // Extract room names
            var roomNames = roomList.Select(r => r.Name).ToList();

            // Sort room names
            roomNames.Sort();

            var ranges = new List<string>();
            string start = roomNames[0];
            string end = start;

            for (int i = 1; i < roomNames.Count; i++)
            {
                string currentRoom = roomNames[i];
                string previousRoom = roomNames[i - 1];

                // Check if the current room is consecutive to the previous one
                if (IsConsecutive(previousRoom, currentRoom))
                {
                    end = currentRoom;
                }
                else
                {
                    // Add the current range or single room
                    ranges.Add(FormatRange(start, end));
                    start = currentRoom;
                    end = currentRoom;
                }
            }

            // Add the last range
            ranges.Add(FormatRange(start, end));

            //return string.Join(", ", ranges);
            return ranges;
        }

        private bool IsConsecutive(string previousRoom, string currentRoom)
        {
            try
            {
                // Extract numeric part of the room strings
                int prevNumber = int.Parse(new string(previousRoom.Where(char.IsDigit).ToArray()));
                int currNumber = int.Parse(new string(currentRoom.Where(char.IsDigit).ToArray()));

                // Check if the difference is 1
                return currNumber - prevNumber == 1;
            }
            catch (FormatException ex)
            {
                // Handle case where room number is not a valid integer
                Console.WriteLine($"Error parsing room numbers: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return false;
            }
        }


        private string FormatRange(string start, string end)
        {
            // Return as a range if start and end are different, otherwise return the single room
            return start == end ? start : $"{start} - {end}";
        }

        public List<string> GetAllRoomId(string roomTypeId)
        {
            var idList = new List<string>();
            foreach (var room in GetAllRooms().Where(r => r.RoomTypeId == roomTypeId && r.Status != "Disabled")) // ensure that is under the same room type
            {
                idList.Add(room.Id);
            }
            return idList;
        }

        // Room ids is for available in this month
        // all reservation is for all reservation 
        public string? GetRandomRoomId(string roomId, List<Reservation> allReservations, List<WaitingList> wt, List<string> roomDlt)
        {
            var crrDate = DateTime.Now;
            var roomIdsAvb = new List<string>();
            var multipleAvb = new List<string>();
            var roomIds = new List<string>();
            if (wt.Any())
            {
                foreach (var waitList in wt)
                {
                    if (waitList.OrgRoomTypeId == GetAllRoomsById(roomId).Where(rm => rm.Id == roomId).Select(rm => rm.RoomTypeId).FirstOrDefault())
                        multipleAvb.Add(waitList.RoomId);
                }
            }
            else multipleAvb = null;
            var allRoomWithinRT = new List<Rooms>();
            if (multipleAvb != null)
            {
                allRoomWithinRT = GetAllRoomByCategoryId(GetRoomById(roomId).RoomTypeId).Where(rm => rm.Status != "Disabled" && !multipleAvb.Contains(rm.Id) && !roomDlt.Contains(rm.Id)).ToList();
            }
            else
            {
                allRoomWithinRT = GetAllRoomByCategoryId(GetRoomById(roomId).RoomTypeId).Where(rm => rm.Status != "Disabled" && !roomDlt.Contains(rm.Id)).ToList();
            }


            foreach (var reservation in allReservations)
            {
                if (reservation.CheckInDate > crrDate && reservation.RoomId == roomId)
                {
                    roomIds.Add(reservation.RoomId); // Count have how many 
                }

                if (reservation.CheckInDate > crrDate && reservation.RoomId != roomId && allRoomWithinRT.Any(rm => rm.Id == reservation.RoomId))
                {
                    if (!roomIdsAvb.Contains(reservation.RoomId))
                        roomIdsAvb.Add(reservation.RoomId);
                }
            }





            //foreach (var roomId in roomIds)
            //{
            if (roomIds.Count == 1) // check in the list have the delete things if not, need to find the first avb
            {
                // Not have any data in future so that can be replace directly                     

                //return "null";
                //return roomIdsAvb.First();
            return string.Join(",", roomIdsAvb);
            }
            else
            {
                foreach (var rm in allRoomWithinRT)
                {
                    if (!roomIdsAvb.Contains(rm.Id) && rm.Id != roomId)
                    {
                        return rm.Id;
                    }
                }

                // represent the future have this room already
                //multipleAvb.Add(roomId);
            }
            //}

            return string.Join(",", roomIdsAvb);
            //return null; // No available room found
        }

        //public string? GetRandomRoomId(string roomId, List<Reservation> allReservations, List<WaitingList> wt, List<string> roomDlt)
        //{
        //    var crrDate = DateTime.Now;
        //    var roomIdsAvb = new List<string>();
        //    var multipleAvb = wt.Select(w => w.RoomId).ToList();
        //    var allRoomWithinRT = GetAllRoomByCategoryId(GetRoomById(roomId).RoomTypeId)
        //        .Where(rm => rm.Status != "Disabled" && !multipleAvb.Contains(rm.Id) && !roomDlt.Contains(rm.Id))
        //        .ToList();

        //    foreach (var room in allRoomWithinRT)
        //    {
        //        var hasOverlap = allReservations.Any(reservation =>
        //            reservation.RoomId == room.Id &&
        //            reservation.CheckOutDate > crrDate); // Check if the room is reserved for the future

        //        if (!hasOverlap)
        //        {
        //            roomIdsAvb.Add(room.Id);
        //        }
        //    }
        //    //    return string.Join(",", roomIdsAvb);

        //    return roomIdsAvb.FirstOrDefault(); // Return the first available room, or null if none are found
        //}


    }
}
