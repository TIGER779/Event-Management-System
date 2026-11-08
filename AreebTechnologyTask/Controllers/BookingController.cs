using System.Security.Claims;
using AreebTechnologyTask.Enums;
using Microsoft.AspNetCore.Authorization;
using AreebTechnologyTask.Data;
using Microsoft.AspNetCore.Mvc;
using AreebTechnologyTask.Services.Tickets;
using Microsoft.EntityFrameworkCore;

namespace AreebTechnologyTask.Controllers
{
    [Authorize(Roles = nameof(UserRole.User))]
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TicketCodeService _ticketCodes;
        private readonly TicketDocumentService _ticketDocuments;
        private readonly TicketEmailQueue _ticketEmails;

        public BookingController(AppDbContext context, TicketCodeService ticketCodes,
            TicketDocumentService ticketDocuments, TicketEmailQueue ticketEmails)
        {
            _context = context;
            _ticketCodes = ticketCodes;
            _ticketDocuments = ticketDocuments;
            _ticketEmails = ticketEmails;
        }

        // The logged-in user's ID from the validated JWT (0 if missing).
        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        // This site's address (e.g. https://localhost:5025), used for the QR code link on tickets.
        private string SiteBaseUrl => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        // POST: /Booking/ResendTicket - emails the ticket again (e.g. if the first email did not arrive).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendTicket(int bookingId)
        {
            var ownsBooking = await _context.Bookings.AnyAsync(b => b.Id == bookingId && b.UserId == CurrentUserId);
            if (!ownsBooking)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction(nameof(MyBookings));
            }

            _ticketEmails.Enqueue(bookingId);
            TempData["Success"] = $"Your ticket is on its way to {User.FindFirstValue(ClaimTypes.Email)}.";
            return RedirectToAction(nameof(MyBookings));
        }

        // GET: /Booking/TicketPdf/5 - downloads the PDF ticket for one of the user's own bookings.
        [HttpGet]
        public async Task<IActionResult> TicketPdf(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == CurrentUserId);
            if (booking == null)
            {
                return NotFound();
            }

            var ticket = TicketInfo.From(booking, _ticketCodes, SiteBaseUrl);
            return File(_ticketDocuments.CreatePdf(ticket), "application/pdf", $"ticket-{ticket.TicketCode}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdClaim);

            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Where(b => b.UserId == userId)
                .ToListAsync();

            // Signed ticket code and check-ticket link for each booking (used by the "Ticket" image's QR code).
            ViewBag.TicketCodes = bookings.ToDictionary(b => b.Id, b => _ticketCodes.CreateCode(b.Id));
            ViewBag.VerifyBaseUrl = $"{SiteBaseUrl}/Tickets/Verify?code=";

            return View(bookings); //Views/Booking/MyBookings.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdClaim);

            // Find the booking and ensure it belongs to the current user
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found or you don't have permission to cancel it.";
                return RedirectToAction(nameof(MyBookings));
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking cancelled successfully!";
            return RedirectToAction(nameof(MyBookings));
        }

        [HttpGet]
        public async Task<IActionResult> Congratulations(string eventName)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.Name == eventName);
            if (!eventExists)
            {
                return NotFound("Event not found.");
            }

            ViewBag.EventName = eventName;
            return View();
        }
    }
}

