using HotelRoomReservationSystem.Models;
using static System.Net.Mime.MediaTypeNames;
using HotelRoomReservationSystem.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelRoomReservationSystem.DAL
{
    public class RoomTypeImageRepository : IRoomTypeImageRepository
    {
        private readonly HotelRoomReservationDB db;

        public RoomTypeImageRepository(HotelRoomReservationDB db)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
        }

        //public int GetLatestSequence()
        //{
        //    //string datePrefix = DateTime.Now.ToString("yyMMdd");
        //    //var latestId = db.RoomTypeImages
        //    //                 .Where(r => r.Id.StartsWith(datePrefix))
        //    //                 .OrderByDescending(r => r.Id)
        //    //                 .Select(r => r.Id)
        //    //                 .FirstOrDefault();

        //    //if (latestId == null) return 0;

        //    //// Extract the numeric part of the ID (the sequence)
        //    //return int.Parse(latestId.Substring(datePrefix.Length));
        //}

        public int SaveRoomTypeImages(RoomTypeImages roomTypeImage)
        {
            // Add the new room image to the RoomTypeImagess table
            db.RoomTypeImages.Add(roomTypeImage);
            return db.SaveChanges();  // Save changes to the database
        }

        public List<RoomTypeImages> GetAllDataList()
        {
            return db.RoomTypeImages.ToList();
        }
        public List<RoomTypeImages> GetRoomTypeImagesById(string id)
        {
            return db.RoomTypeImages.Where(img => img.RoomTypeId == id).ToList();
        }
        public List<string> GetNameById(string id)
        {
            return db.RoomTypeImages.Where(img => img.RoomTypeId == id).Select(img => img.Name).ToList();
        }

        public int Remove(string id)
        {
            db.RoomTypeImages.Where(img => img.RoomTypeId == id).ExecuteDelete();
            return db.SaveChanges();
        }
    }
}
