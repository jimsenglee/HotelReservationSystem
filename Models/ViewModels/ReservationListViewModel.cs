namespace HotelRoomReservationSystem.Models.ViewModels
{
    public class ReservationListViewModel
    {
        public string ReservationId { get; set; }
        public DateTime? CheckInDate { get; set; } // Allow NULL
        public DateTime? CheckOutDate { get; set; } // Allow NULL
        public decimal TotalPrice { get; set; }
        public decimal TransactionAmount { get; set; }
        public string Status { get; set; }

        // Related Room Details
        public string RoomName { get; set; }
        public int? RoomCapacity { get; set; } // Allow NULL

        // Related User Details
        public string UserName { get; set; }
        public string UserEmail { get; set; }
    }

}
