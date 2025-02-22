using HotelRoomReservationSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelRoomReservationSystem.DAL.Interfaces;
using Humanizer;
using Microsoft.EntityFrameworkCore.Storage;


namespace HotelRoomReservationSystem.DAL
{
    public class RoomRepository : IRoomRepository
    {
        private readonly HotelRoomReservationDB db;
        private IDbContextTransaction _currentTransaction;
        public RoomRepository(HotelRoomReservationDB db)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public string getLast()
        {
            return db.Rooms.Max(r => r.Id);
        }
        public List<Rooms> GetAllRooms()
        {
            return db.Rooms.ToList();
        }

        public List<Rooms> GetRoomsById(string searchBar)
        {
            return db.Rooms
         .Where(room => room.Name.Contains(searchBar) || room.Id.ToString().Contains(searchBar))
         .ToList();
        }
        public Rooms GetDataByName(string roomName)
        {
            return db.Rooms.Find(roomName);
        }

        public Rooms GetRoom(string roomId)
        {
            return db.Rooms.Find(roomId);//.Include(r => r.Category)       // Include related Category//.Include(r => r.RoomImages)    // Include related RoomImages// Find the room by ID
        }

        public bool CheckId(string id)
        {
            return !db.Rooms.Any(r => r.Id == id);
        }

        public bool CheckName(string name)
        {
            return !db.Rooms.Any(r => r.Name == name);
        }

        public int SaveRoom(Rooms room)
        {
            db.Rooms.Add(room);
            return db.SaveChanges();
        }

        public int updateData(Rooms room)
        {
            var existingRoom = db.Rooms.FirstOrDefault(r => r.Id == room.Id);

            if (existingRoom != null)
            {
                // Update the existing room's properties
                existingRoom.DateCreated = room.DateCreated;
                existingRoom.Status = room.Status;
                existingRoom.RoomTypeId = room.RoomTypeId;
                existingRoom.Name = room.Name;

                return 1;
            }
            else
            {
                return 0;
            }
        }

        //public void Delete(string roomId)
        //{
        //    var room = db.Rooms.SingleOrDefault(r => r.Id == roomId);
        //    if (room != null)
        //    {
        //        db.Rooms.Remove(room);
        //    }
        //    else
        //    {
        //        throw new ArgumentException($"Room with ID {roomId} not found.");
        //    }
        //}
        //public void BeginTransaction()
        //{
        //    if (_currentTransaction == null)
        //    {
        //        _currentTransaction = db.Database.BeginTransaction();
        //    }
        //}

        //public void CommitTransaction()
        //{
        //    if (_currentTransaction != null)
        //    {
        //        _currentTransaction.Commit();
        //        _currentTransaction.Dispose();
        //        _currentTransaction = null; // Reset transaction
        //    }
        //}


        //public void RollbackTransaction()
        //{
        //    if (_currentTransaction != null)
        //    {
        //        _currentTransaction.Rollback();
        //        _currentTransaction.Dispose();
        //        _currentTransaction = null; // Reset transaction
        //    }
        //}

        //public void SaveChanges()
        //{
        //    db.SaveChanges();
        //}

        public void BeginTransaction()
        {
            _currentTransaction = db.Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            _currentTransaction?.Commit();
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }

        public void RollbackTransaction()
        {
            _currentTransaction?.Rollback();
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }

        public void SaveChanges()
        {
            db.SaveChanges();
        }

        public void Delete(string roomId)
        {
            var room = db.Rooms.SingleOrDefault(r => r.Id == roomId);
            if (room != null)
            {
                db.Rooms.Remove(room);
            }
            else
            {
                throw new ArgumentException($"Room with ID {roomId} not found.");
            }
        }

        public List<Rooms> GetDataByCtgId(string catgeoryId)
        {
            return db.Rooms
                .Where(room => room.RoomTypeId.Contains(catgeoryId))
                .ToList();
        }

        public bool Update(Rooms room)
        {
            var existingRoomType = db.Rooms.FirstOrDefault(rt => rt.Id == room.Id);
            if (existingRoomType != null)
            {
                existingRoomType.Status = room.Status;

                db.SaveChanges();
                return true;
            }
            return false;
        }
    }
}
