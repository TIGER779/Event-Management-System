using EventHub.Enums;

namespace EventHub.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string HashedPassword { get; set; }
        public UserRole Role { get; set; } = UserRole.User;
        public string Address { get; set; }
        public string Phone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Booking>? Bookings { get; set; }


    }
}
