namespace HotelRoomReservationSystem.Models.ViewModels
{
    public class RTWithImgVM
    {
        public string RtId { get; set; }
        public string RtName { get; set; }

        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Images { get; set; }
        
    }
}
