using DocumentFormat.OpenXml.Drawing.Diagrams;
using HotelRoomReservationSystem.BLL.Interfaces;
using HotelRoomReservationSystem.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Stripe;
using System.Text.RegularExpressions;

namespace HotelRoomReservationSystem.BLL
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository reservationRepository;
        public ReservationService(IReservationRepository reservationRepository)
        {
            this.reservationRepository = reservationRepository;
        }

        public List<Reservation> GetAllReservation()
        {
            return reservationRepository.GetAll();
        }

        public List<Reservation> GetAllReservationByRoomId(string id)
        {
            return reservationRepository.GetAll(id);
        }
        //public Reservation GetReservationByRoomId(string id)
        //{
        //    if (id == null) return null;

        //    return reservationRepository.GetData(id);
        //}

        public bool CanDeleteOrChangeRoom(string roomId, DateTime currentDate)
        {
            // Retrieve current and future reservations for the room
            var reservations = GetAllReservationByRoomId(roomId);

            foreach (var reservation in reservations)
            {
                if (reservation.CheckInDate <= currentDate && reservation.CheckOutDate >= currentDate)
                {
                    // Room is currently reserved or occupied
                    return false;
                }
            }

            // No conflicts found
            return true;
        }

        //public void ReAssignRoom(string dltRoomValue, string newRoomId, List<string> allRooms)
        //{
        //    // Get reservations for the current room being deleted
        //    var currentReservations = GetAllReservationByRoomId(dltRoomValue);
        //    var currentDate = DateTime.Now;

        //    // Get reservations for the new room
        //    var newRoomReservations = GetAllReservationByRoomId(newRoomId);

        //    // Collect the existing schedules for the new room
        //    var newRoomSchedule = newRoomReservations
        //        .SelectMany(reservation => Enumerable.Range(0, (reservation.CheckOutDate - reservation.CheckInDate).Days + 1)
        //                                              .Select(offset => reservation.CheckInDate.AddDays(offset)))
        //        .ToHashSet(); // Use HashSet for quick lookup

        //    foreach (var reservation in currentReservations)
        //    {
        //        // Only consider future reservations for reassignment
        //        if (reservation.CheckInDate > currentDate)
        //        {
        //            // Check for schedule conflict with the new room
        //            bool hasConflict = Enumerable.Range(0, (reservation.CheckOutDate - reservation.CheckInDate).Days + 1)
        //                                         .Any(offset => newRoomSchedule.Contains(reservation.CheckInDate.AddDays(offset)));

        //            if (hasConflict)
        //            {
        //                // Find an alternative room without conflicts
        //                var alternativeRoomId = allRooms.FirstOrDefault(roomId =>
        //                {
        //                    if (roomId == newRoomId || roomId == dltRoomValue) return false; // Skip the current and new room
        //                    var alternativeRoomReservations = GetAllReservationByRoomId(roomId);
        //                    var alternativeRoomSchedule = alternativeRoomReservations
        //                        .SelectMany(r => Enumerable.Range(0, (r.CheckOutDate - r.CheckInDate).Days + 1)
        //                                                   .Select(offset => r.CheckInDate.AddDays(offset)))
        //                        .ToHashSet();

        //                    // Check if this room has no conflict
        //                    return !Enumerable.Range(0, (reservation.CheckOutDate - reservation.CheckInDate).Days + 1)
        //                                      .Any(offset => alternativeRoomSchedule.Contains(reservation.CheckInDate.AddDays(offset)));
        //                });

        //                if (!string.IsNullOrEmpty(alternativeRoomId))
        //                {
        //                    reservation.RoomId = alternativeRoomId;
        //                }
        //                else
        //                {
        //                    throw new Exception("No available room found for reassigning reservation.");
        //                }
        //            }
        //            else
        //            {
        //                // Assign to the new room since there's no conflict
        //                reservation.RoomId = newRoomId;
        //            }

        //            // Update the reservation in the database
        //            reservationRepository.Update(reservation);
        //        }
        //    }
        //}
        //public string ReAssigned(string oldRoomId, string newRoomId, List<string> allRoomList, List<Reservation> reservations)
        //{
        //    var currentReservations = GetAllReservationByRoomId(oldRoomId).Where(rs => rs.CheckInDate > DateTime.Now);
        //    var reservationCount = currentReservations.Count();
        //    var newRoomReservations = GetAllReservationByRoomId(newRoomId);
        //    var reservateionData = new List<(DateTime CheckIn, DateTime CheckOut, string roomId)>();

        //    // GET ALL THE DATA IN RESERVATION
        //    foreach (var rs in reservations)
        //    {
        //        reservateionData.Add((rs.CheckInDate, rs.CheckOutDate, rs.RoomId));
        //    }

        //    // START TO CHECK DATA
        //    // Split the string into an array of strings
        //    string[] roomIdsArray = newRoomId.Split(", ");
        //    List<string> roomIds = roomIdsArray.ToList();
        //    //int i = 0;
        //    if (roomIds.Count == 0) // Since it is not have crashing
        //    {
        //        foreach (var reservation in currentReservations)
        //        {
        //            // Assign new room ID to the reservation 
        //            reservation.RoomId = newRoomId;
        //            reservationRepository.Update(reservation);
        //        }
        //    }
        //    else // got multiple value since it is crushing
        //    {

        //        bool isValid = false;
        //        int i = 0; // Counter to track valid reservations processed
        //        int iterationCount = 0; // To ensure at least two iterations

        //        do
        //        {
        //            isValid = true; // Assume valid until proven otherwise
        //            foreach (var rId in roomIds)
        //            {
        //                foreach (var reservation in currentReservations)
        //                {
        //                    var matchingReservation = reservateionData.FirstOrDefault(rs => rs.roomId == rId &&
        //                        (rs.CheckOut <= reservation.CheckInDate || rs.CheckIn >= reservation.CheckOutDate));

        //                    if (matchingReservation != default)
        //                    {
        //                        if (isValid)
        //                        {
        //                            // Do nothing on the first iteration (validation only)
        //                            if (iterationCount == 1)
        //                            {
        //                                reservation.RoomId = newRoomId;
        //                                reservationRepository.Update(reservation); // Update the database on the second iteration
        //                            }
        //                            i++; // Increment when a valid reservation is found
        //                        }
        //                        else
        //                        {
        //                            i = -1; // If not valid, reset the counter and break out
        //                            break;
        //                        }
        //                    }
        //                }
        //            }

        //            // Check if all reservations are valid and processed
        //            if (i == reservationCount)
        //            {
        //                isValid = true;
        //            }
        //            else
        //            {
        //                isValid = false;
        //                i = 0; // Reset counter to recheck in next iteration
        //            }

        //            iterationCount++; // Increment the iteration count
        //        } while (iterationCount < 2 || i != reservationCount); // Ensure at least 2 iterations

        //    }


        //    //foreach (var reservation in currentReservations)
        //    //{
        //    //    // Check for conflicts with new room's existing reservations
        //    //    var hasConflict = newRoomReservations.Any(r =>
        //    //        (r.CheckInDate < reservation.CheckOutDate && r.CheckOutDate > reservation.CheckInDate)); // Overlap condition

        //    //    if (!hasConflict)
        //    //    {
        //    //        // Assign new room ID to the reservation
        //    //        reservation.RoomId = newRoomId;
        //    //        reservationRepository.Update(reservation);
        //    //    }
        //    //    else
        //    //    {
        //    //        throw new Exception($"Cannot reassign room. Conflict detected with room ID {newRoomId}.");
        //    //    }
        //    //}
        //}


        public string ReAssigned(string oldRoomId, string newRoomId, List<string> allRoomList, List<Reservation> reservations)
        {
            var currentReservations = GetAllReservationByRoomId(oldRoomId).Where(rs => rs.CheckInDate > DateTime.Now).ToList();
            var reservationCount = currentReservations.Count();
            var newRoomReservations = GetAllReservationByRoomId(newRoomId);
            var v = "";
            //foreach(var s in currentReservations)
            //{
            //    v += s.RoomId + ", ";
            //}
            //return $"{v}";
            bool isValid = false;
            //var reservationData = new List<(DateTime CheckIn, DateTime CheckOut, string roomId)>();

            // Get all the reservation data for validation
            //foreach (var rs in reservations)
            //{
            //    if (rs.RoomId == oldRoomId)
            //        reservationData.Add((rs.CheckInDate, rs.CheckOutDate, rs.RoomId));
            //}
            //var reservationData = reservations.Select(rs => (rs.CheckInDate, rs.CheckOutDate, rs.RoomId)).ToList();
            var reservationData = reservations.Select(rs => (rs.CheckInDate, rs.CheckOutDate, rs.RoomId)).Where(rs => rs.CheckInDate > DateTime.Now && rs.CheckOutDate > DateTime.Now).ToList();
            // Split newRoomId into a list of room IDs
            var roomIds = newRoomId.Split(",").ToList();
            //List<string> = roomIdsArray.ToList();
            //return $"{newRoomId}";

            int i = 0; // Counter for successfully validated reservations
            int iterationCount = 0; // Ensure at least two iterations

            //return $"{roomIds.Count}";
            // If there are no room IDs (single room), directly proceed with assignment
            if (roomIds.Count == 1)
            {
                iterationCount = 0;
                var s = "";
                do
                {
                    s += "dp, ";
                    //return "false";
                    i = 0; // Reset the counter at the start of each iteration
                    var updatedReservations = new List<(DateTime CheckIn, DateTime CheckOut, string RoomId)>(reservationData);
                    foreach (var reservation in currentReservations)
                    {
                        var overlappingReservation = updatedReservations.FirstOrDefault(rs => rs.RoomId == newRoomId &&
                               rs.CheckOut > reservation.CheckInDate && rs.CheckIn < reservation.CheckOutDate);  // CHECK WHETHER IS IT OVERALP OR NOT BASED ON SAME ID, AND BETWEEN THE START AND END
                                                                                                                 //return "hashjdksah";
                        if (overlappingReservation == default) // No overlap found
                        {
                            s += "1, ";
                            if (isValid)
                            {
                                updatedReservations.Add((reservation.CheckInDate, reservation.CheckOutDate, newRoomId));  // UPDATE THE LASTEST DATA, SAME WITH UPDATE METHODS
                                s += "4, ";
                                reservation.RoomId = newRoomId;
                                reservationRepository.Update(reservation); // First and only update
                                //return "hahah";
                                //break;
                            }
                            else
                            {
                                s += "2, ";
                                i++;
                            }
                        }
                    }

                    //return $"{i}, {updatedReservations.Count}, {reservationCount}, {isValid}";
                    if (i >= reservationCount && iterationCount == 0)
                    {
                        isValid = true; // All reservations are valid
                                        //return $"{isValid}";
                    }

                    iterationCount++; // Increment the iteration count
                } while (iterationCount < 2);
                return $"{s}";

            }
            else // Handle multiple rooms scenario
            {
                                iterationCount = 0;
                do
                {
                    i = 0; // Reset the counter at the start of each iteration
                    var updatedReservations = new List<(DateTime CheckIn, DateTime CheckOut, string RoomId)>(reservationData);
                    foreach (var reservation in currentReservations)
                    {
                        foreach (var rId in roomIds)
                        {
                            var overlappingReservation = updatedReservations.FirstOrDefault(rs => rs.RoomId == rId &&
                                rs.CheckOut > reservation.CheckInDate && rs.CheckIn < reservation.CheckOutDate);  // CHECK WHETHER IS IT OVERALP OR NOT BASED ON SAME ID, AND BETWEEN THE START AND END
                          
                            if (overlappingReservation == default) // No overlap found
                            {
                                if (isValid)
                                {
                                    updatedReservations.Add((reservation.CheckInDate, reservation.CheckOutDate, rId));  // UPDATE THE LASTEST DATA, SAME WITH UPDATE METHODS
                                    reservation.RoomId = rId;
                                    reservationRepository.Update(reservation); // Update room assignment

                                }
                                else
                                {
                                    i++; // Increment for each valid assignment found
                                }
                            }
                        }
                    }

                    // Validate all reservations
                    if (i >= reservationCount && iterationCount == 0)
                    {
                        isValid = true; // All reservations processed
                    }

                    iterationCount++; // Increment the iteration count
                } while (iterationCount < 2); // Ensure at least 2 iterations or until all reservations are valid

            }
            //return "false";

            return isValid ? null : "Failed to reassign rooms due to conflicts." + v;
        }

        //public string ReAssigned(string oldRoomId, string newRoomId, List<string> allRoomList, List<Reservation> reservations)
        //{
        //    var currentReservations = GetAllReservationByRoomId(oldRoomId).Where(rs => rs.CheckInDate > DateTime.Now).ToList();
        //    var reservationCount = currentReservations.Count();
        //    var reservationData = reservations.Select(rs => (rs.CheckInDate, rs.CheckOutDate, rs.RoomId)).Where(rs => rs.CheckInDate > DateTime.Now).ToList();
        //    var roomIds = newRoomId.Split(",").ToList();
        //    //return "false";
        //    int iterationCount = 0;
        //    bool isValid = false;

        //    if (roomIds.Count == 1)
        //    {
        //        ////return $"{currentReservations.Count}";
        //        foreach (var reservation in currentReservations)
        //        {
        //            reservation.RoomId = newRoomId;
        //            //return reservation.Id;
        //            reservationRepository.Update(reservation);
        //            //return "asndsand";
        //        }
        //        return null; // Success
        //        //try
        //        //{
        //        //    foreach (var reservation in currentReservations)
        //        //    {
        //        //        reservation.RoomId = roomIds[0];
        //        //        return roomIds[0];
        //        //        reservationRepository.Update(reservation);
        //        //    }

        //        //}
        //        //catch (Exception ex)
        //        //{
        //        //    //transaction.Rollback();
        //        //    return $"Error: {ex.Message}";
        //        //}
        //    }

        //    do
        //    {
        //        //return "hwhat";
        //        var updatedReservations = new List<(DateTime CheckIn, DateTime CheckOut, string RoomId)>(reservationData);
        //        int successfullyReassigned = 0;

        //        foreach (var reservation in currentReservations)
        //        {
        //            foreach (var rId in roomIds)
        //            {
        //                var hasOverlap = updatedReservations.Any(rs => rs.RoomId == rId &&
        //                    rs.CheckOut > reservation.CheckInDate && rs.CheckIn < reservation.CheckOutDate);

        //                if (!hasOverlap)
        //                {
        //                    updatedReservations.Add((reservation.CheckInDate, reservation.CheckOutDate, rId));
        //                    reservation.RoomId = rId;
        //                    reservationRepository.Update(reservation);
        //                    successfullyReassigned++;
        //                    break;
        //                }
        //            }
        //        }

        //        if (successfullyReassigned == reservationCount)
        //        {
        //            isValid = true; // All reservations reassigned successfully
        //            break;
        //        }

        //        iterationCount++;
        //    } while (iterationCount < 2);

        //    return isValid ? null : "Failed to reassign rooms due to conflicts.";
        //}

        public Reservation getReservationById(string reservationId)
        {
            return reservationRepository.GetById(reservationId);
        }
    }
}
