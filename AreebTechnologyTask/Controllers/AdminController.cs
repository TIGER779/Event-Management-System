using EventHub.Data;
using EventHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventHub.Enums;
using Microsoft.AspNetCore.Authorization;

namespace EventHub.Controllers
{
    [Authorize(Roles = nameof(UserRole.Admin))]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/AddEvent
        public IActionResult AddEvent()
        {
            return View(); //Views/Admin/AddEvent.cshtml
        }

        // POST: /Admin/AddEvent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEvent(Event newEvent)
        {
            if (ModelState.IsValid)
            {
                _context.Events.Add(newEvent);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Event added successfully!";
                return RedirectToAction(nameof(AdminPanel));
            }
            return View(newEvent);
        }


        // GET: /Admin/EditEvent/5
        public async Task<IActionResult> EditEvent(int id)
        {
            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null)
                return NotFound();

            return View(existingEvent); //Views/Admin/EditEvent.cshtml
        }

        // POST: /Admin/EditEvent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, Event updatedEvent)
        {
            if (id != updatedEvent.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(updatedEvent);

            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null)
                return NotFound();

            existingEvent.Name = updatedEvent.Name;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.Category = updatedEvent.Category;
            existingEvent.Date = updatedEvent.Date;
            existingEvent.Venue = updatedEvent.Venue;
            existingEvent.Price = updatedEvent.Price;
            existingEvent.ImageUrl = updatedEvent.ImageUrl;

            await _context.SaveChangesAsync();

            // success message
            TempData["SuccessMessage"] = "Event updated successfully!";

            return RedirectToAction(nameof(AdminPanel));
        }


        // POST: /Admin/DeleteEvent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null)
                return NotFound();

            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();

            // success message
            TempData["SuccessMessage"] = "Event Deleted successfully!";

            return RedirectToAction(nameof(AdminPanel));
        }

        // GET: /Admin/Index
        [HttpGet]
        public async Task<IActionResult> AdminPanel()
        {
            var events = await _context.Events.ToListAsync();
            return View(events); // Views/Event/Index.cshtml
        }


    }
}


// Admin Side Event Controller (CRUDs)
/*
 * 1-  GET / (list all events)
 * 2-  POST / (admin only - create)
 * 3-  PUT /{id} (admin only - update)
 * 4-  DELETE /{id} (admin only - delete)
 */

// User Side Event Controller
/*   1 - GET / (list all events)
 *   2-  GET /{id} (get event by id)*/


