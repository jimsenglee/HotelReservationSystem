// import classes in the Models folder to the entire project
global using HotelRoomReservationSystem.Models;
global using Demo;
using Twilio.Clients;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews(); // Required for MVC pattern
builder.Services.AddScoped<Helper>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
    options.Cookie.HttpOnly = true; // Ensure cookies are server-side only
    options.Cookie.IsEssential = true; // Mark session cookie as essential
});

// Register application services and repositories with Dependency Injection
builder.Services.AddScoped<HotelRoomReservationSystem.BLL.Interfaces.IRoomService, HotelRoomReservationSystem.BLL.RoomService>();
builder.Services.AddScoped<HotelRoomReservationSystem.BLL.Interfaces.IRoomTypeImageService, HotelRoomReservationSystem.BLL.RoomTypeImageService>();
builder.Services.AddScoped<HotelRoomReservationSystem.BLL.Interfaces.IRoomTypeService, HotelRoomReservationSystem.BLL.RoomTypeService>();
builder.Services.AddScoped<HotelRoomReservationSystem.BLL.Interfaces.IReservationService, HotelRoomReservationSystem.BLL.ReservationService>();
builder.Services.AddScoped<HotelRoomReservationSystem.BLL.Interfaces.IWaitingListService, HotelRoomReservationSystem.BLL.WaitingListService>();
builder.Services.AddScoped<HotelRoomReservationSystem.BLL.Interfaces.IFeedbackService, HotelRoomReservationSystem.BLL.FeedbackService>();
builder.Services.AddScoped<HotelRoomReservationSystem.BLL.Interfaces.IFeedbackMediaService, HotelRoomReservationSystem.BLL.FeedbackMediaService>();
builder.Services.AddScoped<HotelRoomReservationSystem.BLL.Interfaces.IUserService, HotelRoomReservationSystem.BLL.UserService>();
builder.Services.AddScoped<HotelRoomReservationSystem.BLL.Interfaces.IRewardsService, HotelRoomReservationSystem.BLL.RewardsService>();
builder.Services.AddScoped<HotelRoomReservationSystem.BLL.Interfaces.IMembershipService, HotelRoomReservationSystem.BLL.MembershipService>();
builder.Services.AddScoped<HotelRoomReservationSystem.BLL.Interfaces.IMembershipRewardsService, HotelRoomReservationSystem.BLL.MembershipRewardsService>();

builder.Services.AddScoped<HotelRoomReservationSystem.DAL.Interfaces.IRoomRepository, HotelRoomReservationSystem.DAL.RoomRepository>();
builder.Services.AddScoped<HotelRoomReservationSystem.DAL.Interfaces.IRoomTypeImageRepository, HotelRoomReservationSystem.DAL.RoomTypeImageRepository>();
builder.Services.AddScoped<HotelRoomReservationSystem.DAL.Interfaces.IRoomTypeRepository, HotelRoomReservationSystem.DAL.RoomTypeRepository>();
builder.Services.AddScoped<HotelRoomReservationSystem.DAL.Interfaces.IReservationRepository, HotelRoomReservationSystem.DAL.ReservationRepository>();
builder.Services.AddScoped<HotelRoomReservationSystem.DAL.Interfaces.IWaitingListRepository, HotelRoomReservationSystem.DAL.WaitingListRepository>();
builder.Services.AddScoped<HotelRoomReservationSystem.DAL.Interfaces.IFeedbackRepository, HotelRoomReservationSystem.DAL.FeedbackRepository>();
builder.Services.AddScoped<HotelRoomReservationSystem.DAL.Interfaces.IFeedbackMediaRepository, HotelRoomReservationSystem.DAL.FeedbackMediaRepository>();
builder.Services.AddScoped<HotelRoomReservationSystem.DAL.Interfaces.IUserRepository, HotelRoomReservationSystem.DAL.UserRepository>();
builder.Services.AddScoped<HotelRoomReservationSystem.DAL.Interfaces.IRewardsRepository, HotelRoomReservationSystem.DAL.RewardsRepository>();
builder.Services.AddScoped<HotelRoomReservationSystem.DAL.Interfaces.IMembershipRepository, HotelRoomReservationSystem.DAL.MembershipRepository>();
builder.Services.AddScoped<HotelRoomReservationSystem.DAL.Interfaces.IMembershipRewardsRepository, HotelRoomReservationSystem.DAL.MembershipRewardsRepository>();


// Configure SQL Server database connection
builder.Services.AddSqlServer<HotelRoomReservationDB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\DB.mdf;Integrated Security=True;Connect Timeout=30;
");

builder.Services.AddSession();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        options.JsonSerializerOptions.MaxDepth = 64; // Increase the max depth if necessary
    });
builder.Services.AddScoped<Helper>();

builder.Services.AddAuthentication().AddCookie();
builder.Services.AddHttpContextAccessor(); // For Program.cs

//FOR SMS 

builder.Services.AddControllers();

builder.Services.AddHttpClient<ITwilioRestClient, TwilioClient>();

//FOR SMS

// Build the application
var app = builder.Build();

// Configure middleware and HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Enforce HTTPS in production
}

//app.UseHttpsRedirection();
app.UseStaticFiles(); // Serve static files like CSS, JavaScript, and images
app.UseSession();
app.UseRouting();     // Enable routing
app.UseAuthorization(); // Enable authentication and authorization (if applicable)
app.UseHttpsRedirection();
app.MapControllers();
// Map controller routes
app.MapDefaultControllerRoute(); // Use default route pattern {controller=Home}/{action=Index}/{id?}

// Run the application
app.Run();