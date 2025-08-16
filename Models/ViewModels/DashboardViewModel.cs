namespace HotelRoomReservationSystem.Models.ViewModels;
public class DashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public int TotalReservation { get; set; }
    public List<string> weekLabels { get; set; }

    //Sales Data
    public List<decimal> weeklySalesData { get; set; }
    public List<decimal> monthlySalesData { get; set; }

    //Bookings Data
    public List<int> weeklyBookingsData { get; set; }
    public List<int> monthlyBookingsData { get; set; }

    //Occupancy
    public List<decimal> GetDailyOccupancyRates { get; set; }

    //Other
    public List<decimal> moneyEarnInEachTime { get; set; }

    public List<int> reservationEarnInEachTime { get; set; }

    public List<string> categoryList { get; set; }

    public List<int> topSellingCategory { get; set; }

    public List<Feedback> feedbacks { get; set; }
}


public class CombinationOfCharts()
{
    public string ChartType1 { get; set; }
    public string ChartType2 { get; set; }
    public string DataType1 { get; set; }
    public string DataType2 { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
