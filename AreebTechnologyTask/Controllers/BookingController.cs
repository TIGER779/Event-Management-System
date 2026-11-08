using System.Security.Claims;
using AreebTechnologyTask.Enums;
using Microsoft.AspNetCore.Authorization;
using AreebTechnologyTask.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AreebTechnologyTask.Controllers
{
    [Authorize(Roles = nameof(UserRole.User))]
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;

        public BookingController(AppDbContext context)
        {
            _context = context;
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

