//CHART

// Retrieve dynamic chart titles from the Razor view
let barChartTitle = JSON.parse(document.getElementById('barChartTitle').textContent);

// Get the current month as a string
const today = new Date();
const currentMonth = today.toLocaleString('default', { month: 'long' }); // 'long' gives the full month name (e.g., 'December')


// Inject labels and data (Ensure these variables are provided in your HTML view as a script tag or similar)
const weekLabels = JSON.parse(document.getElementById('weekLabels').textContent);
console.log(weekLabels);

// Sales Data
const weeklySalesData = JSON.parse(document.getElementById('weeklySalesData').textContent);
console.log(weeklySalesData);

const monthlySalesData = JSON.parse(document.getElementById('monthlySalesData').textContent);
console.log(monthlySalesData);


//Bookings Data
const weeklyBookingsData = JSON.parse(document.getElementById('weeklyBookingsData').textContent);
console.log(weeklyBookingsData)

const monthlyBookingsData = JSON.parse(document.getElementById('monthlyBookingsData').textContent);
console.log(monthlyBookingsData)

//Occupancy Rate
const GetDailyOccupancyRates = JSON.parse(document.getElementById('GetDailyOccupancyRates').textContent);
console.log(GetDailyOccupancyRates)

//Revenue Data
const revenueData = JSON.parse(document.getElementById('revenueData').textContent);
console.log(revenueData);

//Reservation Data
const reservationData = JSON.parse(document.getElementById('reservationData').textContent);
console.log(reservationData);

//Top Sell
const categoryList = JSON.parse(document.getElementById('categoryList').textContent);
console.log(categoryList);

const topSellingCategory = JSON.parse(document.getElementById('topSellingCategory').textContent);
console.log(topSellingCategory);


// Define colors for each day
const dayColors = [
    'rgba(255, 99, 132, 0.6)', // Red
    'rgba(255, 159, 64, 0.6)', // Orange
    'rgba(255, 205, 86, 0.6)', // Yellow
    'rgba(75, 192, 192, 0.6)', // Green
    'rgba(54, 162, 235, 0.6)', // Blue
    'rgba(153, 102, 255, 0.6)', // Indigo
    'rgba(201, 203, 207, 0.6)'  // Violet
];





// Event listeners for buttons for Sales
document.getElementById('weeklyBtn').addEventListener('click', () => {
    // Set chart data to weekly
    salesChart.data = weeklyData;
    salesChart.options.plugins.title.text = currentMonth + ' Weekly Sales';
    salesChart.update();

    // Toggle active class on buttons
    document.getElementById('weeklyBtn').classList.add('bg-primary', 'text-white');
    document.getElementById('weeklyBtn').classList.remove('btn-outline-primary');

    document.getElementById('monthlyBtn').classList.remove('bg-primary', 'text-white');
    document.getElementById('monthlyBtn').classList.add('btn-outline-primary');
});

document.getElementById('monthlyBtn').addEventListener('click', () => {
    // Set chart data to monthly
    salesChart.data = monthlyData;
    salesChart.options.plugins.title.text = currentMonth + ' Monthly Sales';
    salesChart.update();

    // Toggle active class on buttons
    document.getElementById('monthlyBtn').classList.add('bg-primary', 'text-white');
    document.getElementById('monthlyBtn').classList.remove('btn-outline-primary');

    document.getElementById('weeklyBtn').classList.remove('bg-primary', 'text-white');
    document.getElementById('weeklyBtn').classList.add('btn-outline-primary');
});


// Bar Chart Configuration
// Sample data for weekly and monthly sales
const weeklyData = {
    labels: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'],
    datasets: [{
        label: currentMonth + ' Weekly Sales', // This is the label for the dataset (appears in the legend)
        data: weeklySalesData,
        backgroundColor: dayColors.slice(0, 7), // Use first 7 colors for weekly data
        borderColor: dayColors.slice(0, 7).map(color => color.replace('0.6', '1')), // Convert to solid colors for border
        borderWidth: 1,
    }]
};

const monthlyData = {
    labels: weekLabels, // You can change this to labels for monthly data if needed
    datasets: [{
        label: currentMonth + ' Monthly Sales', // This is the label for the dataset (appears in the legend)
        data: monthlySalesData,
        backgroundColor: dayColors.slice(0, 5), // Use first 5 colors for monthly data
        borderColor: dayColors.slice(0, 5).map(color => color.replace('0.6', '1')), // Convert to solid colors for border
        borderWidth: 1,
    }]
};

// Bar Chart Configuration
const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        title: {
            display: true,
            text: currentMonth + ' Sales Data'
        },
        legend: {
            display: false, // Make sure the legend is enabled
        },
    }
};

// Initialize the bar chart
const ctxBar = document.getElementById('radarChart').getContext('2d');
let salesChart = new Chart(ctxBar, {
    type: 'bar',
    data: weeklyData, // You can switch between weeklyData and monthlyData based on your need
    options: chartOptions,
});






// Event listeners for buttons for Polar
document.getElementById('polarweeklyBtn').addEventListener('click', () => {
    // Set chart data to weekly
    polarChart.data = polarweeklyData;
    polarChart.options.plugins.title.text = currentMonth + ' Weekly Booking';
    polarChart.update();

    // Toggle active class on buttons
    document.getElementById('polarweeklyBtn').classList.add('bg-primary', 'text-white');
    document.getElementById('polarweeklyBtn').classList.remove('btn-outline-primary');

    document.getElementById('polarmonthlyBtn').classList.remove('bg-primary', 'text-white');
    document.getElementById('polarmonthlyBtn').classList.add('btn-outline-primary');
});

document.getElementById('polarmonthlyBtn').addEventListener('click', () => {
    // Set chart data to monthly
    polarChart.data = polarmonthlyData;
    polarChart.options.plugins.title.text = currentMonth + ' Monthly Booking';
    polarChart.update();

    // Toggle active class on buttons
    document.getElementById('polarmonthlyBtn').classList.add('bg-primary', 'text-white');
    document.getElementById('polarmonthlyBtn').classList.remove('btn-outline-primary');

    document.getElementById('polarweeklyBtn').classList.remove('bg-primary', 'text-white');
    document.getElementById('polarweeklyBtn').classList.add('btn-outline-primary');
});


// Polar Chart Configuration
// Sample data for weekly and monthly Booking
const polarweeklyData = {
    labels: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'], // Set the labels outside of datasets
    datasets: [{
        label: currentMonth + ' Weekly Booking', // Set the label for the dataset
        data: weeklyBookingsData,
        backgroundColor: dayColors.slice(0, 7), // Use first 7 colors for weekly data
        borderColor: dayColors.slice(0, 7).map(color => color.replace('0.6', '1')), // Convert to solid colors for border
        borderWidth: 1,
    }]
};

const polarmonthlyData = {
    labels: weekLabels, // Assuming weekLabels is defined elsewhere for the monthly data
    datasets: [{
        label: currentMonth + ' Monthly Booking', // Label for the dataset
        data: monthlyBookingsData,
        backgroundColor: dayColors.slice(0, 5), // Use first 5 colors for monthly data
        borderColor: dayColors.slice(0, 5).map(color => color.replace('0.6', '1')), // Convert to solid colors for border
        borderWidth: 1,
    }]
};

// Polar Chart Configuration
const chartOptionsPolar = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        title: {
            display: true,
            text: currentMonth + ' Booking Data'
        },
        legend: {
            display: true, // Show the legend
        },
    }
};

// Initialize the polar chart
const ctxPolar = document.getElementById('barChart').getContext('2d');
let polarChart = new Chart(ctxPolar, {
    type: 'polarArea',
    data: polarweeklyData, // Use the correct data object for the weekly data
    options: chartOptionsPolar,
});





function getDaysOfCurrentMonth() {
    // Get the current date
    const today = new Date();

    // Get the current month and year
    const currentMonth = today.getMonth(); // Months are 0-indexed (0 for January, 1 for February, etc.)
    const currentYear = today.getFullYear();

    // Get the number of days in the current month
    const daysInMonth = new Date(currentYear, currentMonth + 1, 0).getDate(); // 0 returns the last day of the previous month

    // Create an array to store the days of the current month
    let daysOfMonth = [];

    // Loop through each day of the current month and add to the list
    for (let day = 1; day <= daysInMonth; day++) {
        daysOfMonth.push(day);
    }

    // Return the array of days
    return daysOfMonth;
}

// Line Chart Configuration
const ctxLine = document.getElementById('occupancyChart').getContext('2d');
new Chart(ctxLine, {
    type: 'line',
    data: {
        labels: getDaysOfCurrentMonth(),
        datasets: [{
            label: 'Occupancy Rate (%)',
            data: GetDailyOccupancyRates,
            borderColor: 'rgba(75, 192, 192, 1)',
            fill: false,
            tension: 0.1
        }]
    },
    options: {
        responsive: true,
        plugins: {
            title: {
                display: true,
                text: 'Occupancy Rate'
            },
            tooltip: {
                callbacks: {
                    label: function (tooltipItem) {
                        return tooltipItem.raw.toFixed(2) + '%';
                    }
                }
            }
        },
        scales: {
            y: {
                beginAtZero: true,
                max: 100
            }
        }
    }
});





//hourly revenue data (for 24 hours)
const ctxRevenue = document.getElementById('revenueChart').getContext('2d');
const hourlyRevenue = {
    labels: [
        '00:00', '01:00', '02:00', '03:00', '04:00', '05:00', '06:00', '07:00', '08:00', '09:00',
        '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00', '18:00', '19:00',
        '20:00', '21:00', '22:00', '23:00'
    ],
    datasets: [{
        label: 'Hourly Revenue',
        data: revenueData,
        backgroundColor: 'rgba(75, 192, 192, 0.2)',
        borderColor: 'rgba(75, 192, 192, 1)',
        borderWidth: 1
    }]
};
new Chart(ctxRevenue, {
    type: 'line',
    data: hourlyRevenue,
    options: {
        responsive: true,
        scales: {
            x: {
                title: {
                    display: true,
                    text: 'Hour of Day'
                },
                ticks: {
                    autoSkip: true,
                    maxTicksLimit: 12
                }
            },
            y: {
                beginAtZero: true,
                title: {
                    display: true,
                    text: 'Revenue (in MRY)'
                }
            }
        }
    }
});



//hourly revenue data (for 24 hours)
const ctxReservation = document.getElementById('reservationChart').getContext('2d');
const hourlyReservation = {
    labels: [
        '00:00', '01:00', '02:00', '03:00', '04:00', '05:00', '06:00', '07:00', '08:00', '09:00',
        '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00', '18:00', '19:00',
        '20:00', '21:00', '22:00', '23:00'
    ],
    datasets: [{
        label: 'Hourly Reservation',
        data: reservationData,
        backgroundColor: 'rgba(75, 192, 192, 0.2)',
        borderColor: 'rgba(75, 192, 192, 1)',
        borderWidth: 1
    }]
};
new Chart(ctxReservation, {
    type: 'line',
    data: hourlyReservation,
    options: {
        responsive: true,
        scales: {
            x: {
                title: {
                    display: true,
                    text: 'Hour of Day'
                },
                ticks: {
                    autoSkip: true,
                    maxTicksLimit: 12
                }
            },
            y: {
                beginAtZero: true,
                title: {
                    display: true,
                    text: 'Number of Reservation'
                },
                ticks: {
                    // Ensure the y-axis uses integer steps
                    stepSize: 1, // This will step by 1 on the y-axis
                    callback: function (value) {
                        return Number.isInteger(value) ? value : ''; // Ensure only integer values are displayed
                    }
                }
            }
        }
    }
});


// Top Sell Rooms
const ctxTopSell = document.getElementById('topSellsChart').getContext('2d');
const topSellRoomTypes = {
    labels: categoryList,
    datasets: [{
        label: 'Top Sells',
        data: topSellingCategory,
        backgroundColor: 'rgba(75, 192, 192, 0.2)',
        borderColor: 'rgba(75, 192, 192, 1)',
        borderWidth: 1
    }]
};
new Chart(ctxTopSell, {
    type: 'bar', // Missing comma added here
    data: topSellRoomTypes,
    options: {
        responsive: true,
        scales: {
            x: {
                title: {
                    display: true,
                    text: 'Room Types'
                },
                ticks: {
                    autoSkip: true,
                    maxTicksLimit: 12
                }
            },
            y: {
                beginAtZero: true,
                title: {
                    display: true,
                    text: 'Number of Rooms'
                },
                ticks: {
                    // Ensure the y-axis uses integer steps
                    stepSize: 1, // This will step by 1 on the y-axis
                    callback: function (value) {
                        return Number.isInteger(value) ? value : ''; // Ensure only integer values are displayed
                    }
                }
            }
        }
    }
});
