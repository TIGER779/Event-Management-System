using System.Diagnostics;
using System.Security.Claims;
using AreebTechnologyTask.Data;
using AreebTechnologyTask.Enums;
using AreebTechnologyTask.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AreebTechnologyTask.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }
        // The logged-in user's ID from the validated JWT (0 when not logged in).
        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        // IDs of the events the current user has booked (empty when not logged in).
        private Task<List<int>> BookedEventIdsAsync() =>
            _context.Bookings
                .Where(b => b.UserId == CurrentUserId)
                .Select(b => b.EventId)
                .ToListAsync();

        // Display all events on the home page
        public async Task<IActionResult> Index(string returnUrl)
        {
            var events = await _context.Events.ToListAsync();

            ViewBag.BookedEventIds = await BookedEventIdsAsync(); // Pass the list of booked event IDs
            ViewBag.ReturnUrl = returnUrl ?? Url.Content("~/");

            return View(events);
        }


        // event booking
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken] // blocks forged booking requests sent from other websites
        public async Task<IActionResult> BookEvent(int eventId)
        {
            var isAjax = Request.Headers.XRequestedWith == "XMLHttpRequest";

            if (User.IsInRole(nameof(UserRole.Admin)))
            {
                if (isAjax)
                    return Json(new { success = false, message = "Admin cannot book events." });

                TempData["Error"] = "Admin cannot book events.";
                return RedirectToAction("Index", "Home");
            }

            var userId = CurrentUserId;
            if (userId == 0)
            {
                if (isAjax)
                    return Json(new { success = false, message = "Invalid token" });

                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Index", "Home") });
            }

            if (!await _context.Events.AnyAsync(e => e.Id == eventId))
            {
                if (isAjax)
                    return Json(new { success = false, message = "Event not found." });

                return NotFound();
            }

            var existingBooking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.EventId == eventId && b.UserId == userId);

            if (existingBooking != null)
            {
                if (isAjax)
                    return Json(new { success = false, message = "Event already booked." });

                TempData["Error"] = "Event already booked.";
                return RedirectToAction("Index", "Home");
            }

            var booking = new Booking
            {
                UserId = userId,
                EventId = eventId,
                BookingDate = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var bookedEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);

            if (isAjax)
            {
                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("Congratulations", "Booking", new { eventName = bookedEvent?.Name })
                });
            }

            TempData["Success"] = "Event booked successfully!";
            return RedirectToAction("Congratulations", "Booking", new { eventName = bookedEvent?.Name });
        }


        // GET: /Event/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (eventItem == null)
            {
                return NotFound();
            }

            ViewBag.IsBooked = (await BookedEventIdsAsync()).Contains(id);
            return View(eventItem); //Views/Event/Details.cshtml
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
