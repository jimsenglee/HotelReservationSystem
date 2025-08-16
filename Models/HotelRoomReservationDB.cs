using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HotelRoomReservationSystem.Models
{
    public class HotelRoomReservationDB : DbContext
    {
        public HotelRoomReservationDB(DbContextOptions<HotelRoomReservationDB> options) : base(options) { }

        public DbSet<Users> Users { get; set; }
        public DbSet<Admin> Admin { get; set; }
        public DbSet<Customer> Customer { get; set; }
        public DbSet<Manager> Manager { get; set; }
        public DbSet<LoginAttempt> LoginAttempt { get; set; }
        public DbSet<Token> Token { get; set; }
        public DbSet<RoomType> RoomType { get; set; }
        public DbSet<RoomTypeImages> RoomTypeImages { get; set; }
        public DbSet<Rooms> Rooms { get; set; }
        public DbSet<Reservation> Reservation { get; set; }
        public DbSet<Transaction> Transaction { get; set; }
        public DbSet<Feedback> Feedback { get; set; }
        public DbSet<FeedbackMedia> FeedbackMedia { get; set; }

        public DbSet<WaitingList> WaitingList { get; set; }

        public DbSet<Rewards> Rewards { get; set; }
        public DbSet<Memberships> Memberships { get; set; }
        public DbSet<MembershipRewards> MembershipRewards { get; set; }
        public DbSet<MFALogin> MFALogin { get; set; }

        public DbSet<Message> Message { get; set; }
    }

}
