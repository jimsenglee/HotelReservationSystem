using HotelRoomReservationSystem.Controllers;
using HotelRoomReservationSystem.DAL.Interfaces;
using HotelRoomReservationSystem.Models;
using HotelRoomReservationSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HotelRoomReservationSystem.DAL
{
    public class RoomTypeRepository: IRoomTypeRepository
    {

        private readonly HotelRoomReservationDB db;

        public RoomTypeRepository(HotelRoomReservationDB db)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public List<RoomType> GetAllDataList()
        {
            return db.RoomType.ToList();
        }
        public List<string> GetRoomTypeNames()
        {
            return db.RoomType.Select(c => c.Name).ToList();
        }
        public RoomType IsSameName(string roomTypeName)
        {
            return db.RoomType.FirstOrDefault(c => c.Name == roomTypeName);
        }

        public bool CheckName(string name)
        {
            return !db.RoomType.Any(r => r.Name == name);
        }

        public RoomType GetRoomTypeById(string Id) {
            return db.RoomType.Where(rt => rt.Id == Id).FirstOrDefault();
        }

        public int SaveRoomType(RoomType roomType)
        {
            db.RoomType.Add(roomType);
            return db.SaveChanges();
        }

        public string getLast()
        {
            return db.RoomType.Max(c => c.Id);
        }

        public List<RoomType> GetAllRoomTypeById(string Id)
        {
            return db.RoomType
        .Where(roomType => roomType.Name.Contains(Id) || roomType.Id.ToString().Contains(Id))
        .ToList();
        }

        public bool Update(RoomType roomType)
        {
            var existingRoomType = db.RoomType.FirstOrDefault(rt => rt.Id == roomType.Id);
            if (existingRoomType != null)
            {
                existingRoomType.Status = roomType.Status;

                db.SaveChanges();
                return true;
            }
            return false;
        }
    }
}
