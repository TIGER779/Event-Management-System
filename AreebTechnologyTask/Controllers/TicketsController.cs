using EventHub.Data;
using EventHub.Services.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Controllers
{
    /// <summary>The page a ticket's QR code opens: tells the entrance staff whether the ticket is valid.</summary>
    [AllowAnonymous]
    public class TicketsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TicketCodeService _ticketCodes;

        public TicketsController(AppDbContext context, TicketCodeService ticketCodes)
        {
            _context = context;
            _ticketCodes = ticketCodes;
        }

        // GET: /Tickets/Verify?code=TKT-000123-K7PQ2MXA4Z
        [HttpGet]
        public async Task<IActionResult> Verify(string? code)
        {
            ViewData["Title"] = "Check Ticket";
            ViewBag.Code = code;

            // A fake or edited code fails the signature check before the database is even asked.
            if (!_ticketCodes.TryReadCode(code, out var bookingId))
            {
                ViewBag.Status = "invalid";
                return View();
            }

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            // A real ticket whose booking was cancelled is no longer valid.
            ViewBag.Status = booking == null ? "cancelled" : "valid";
            return View(booking);
        }
    }
}
