using System.ComponentModel.DataAnnotations.Schema;

namespace EventHub.Models
{
    public class Booking
    {
        public int Id { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        [ForeignKey("Event")]
        public int EventId { get; set; }
        public DateTime BookingDate { get; set; }
        public User User { get; set; }
        public Event Event { get; set; }

    }
}
