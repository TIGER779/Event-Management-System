using EventHub.Models;

namespace EventHub.Services.Tickets
{
    /// <summary>Everything printed on a ticket (email, PDF, calendar file and the ticket image).</summary>
    public record TicketInfo(
        int BookingId,
        string TicketCode,
        string VerifyUrl,
        string HolderName,
        string HolderEmail,
        string EventName,
        string Category,
        DateTime EventDate,
        string Venue,
        int Price,
        string ImageUrl,
        DateTime BookedOnUtc)
    {
        /// <summary>Builds the ticket data for a booking (with its User and Event loaded).</summary>
        public static TicketInfo From(Booking booking, TicketCodeService codes, string publicBaseUrl)
        {
            var code = codes.CreateCode(booking.Id);
            var verifyUrl = $"{publicBaseUrl.TrimEnd('/')}/Tickets/Verify?code={Uri.EscapeDataString(code)}";
            return new TicketInfo(
                booking.Id, code, verifyUrl,
                booking.User.Name, booking.User.Email,
                booking.Event.Name, booking.Event.Category, booking.Event.Date, booking.Event.Venue,
                booking.Event.Price, booking.Event.ImageUrl, booking.BookingDate);
        }
    }
}
