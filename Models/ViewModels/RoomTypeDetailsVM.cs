namespace HotelRoomReservationSystem.Models.ViewModels
{
    public class RoomTypeDetailsVM
    {
        public string RoomTypeId { get; set; }
        public string RoomTypeName { get; set; }

        public string Description{ get; set; }
        public int AvbQuantity { get; set; }
        public int TtlQuantity { get; set; }
        public double Price { get; set; }
        public int Capacity { get; set; }
        public string Status { get; set; }
        public Rooms Rooms{ get; set; } // The full RoomType entity
        public List<RoomTypeImages> Images { get; set; } // List of related RoomImages
    }
}
