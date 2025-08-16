namespace HotelRoomReservationSystem.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public string UsersId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string RoomId { get; set; }
        public string RoomType { get; set; }
        public int? SelectedCapacity { get; set; }
        public decimal RoomPrice { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalRoomPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal TaxAmount { get; set; }  // Tax amount (6%)
        public string? SelectedPaymentMethod { get; set; }  // "Cash" or "PayPal"

        public string? Images {  get; set; }
        public List<Rewards>? rewardsList { get; set; }

        public Memberships? userMember { get; set; }

        public Rewards? reward { get; set; }

        public string? memberId { get; set; }

        public string? memberLevel { get; set; }

        public string? rewardId { get; set; }

        public decimal? rewardDiscount { get; set; }

    }




}
