using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace MovieTheater.Controllers
{
    public class TicketController : Controller
    {
        private readonly MovieTheaterContext _context;

        public TicketController(MovieTheaterContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult History()
        {
            // Redirect /Ticket/History to /Ticket/Index
            return RedirectToAction("Index");
        }
        // AC-01: View all booked tickets
        [HttpGet]
        public IActionResult Index()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = _context.Invoices
                .Where(i => i.AccountId == accountId)
                .OrderByDescending(i => i.BookingDate)
                .ToList();

            return View(bookings);
        }


        // AC-01: View booked tickets with filtering
        [HttpGet]
        public IActionResult Booked()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = _context.Invoices
                .Where(i => i.AccountId == accountId && i.Status == MovieTheater.Models.InvoiceStatus.Completed)
                .OrderByDescending(i => i.BookingDate)
                .ToList();

            return View("Index", bookings);
        }

        // AC-01: View canceled tickets
        [HttpGet]
        public IActionResult Canceled()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = _context.Invoices
                .Where(i => i.AccountId == accountId && i.Status == MovieTheater.Models.InvoiceStatus.Incomplete)
                .OrderByDescending(i => i.BookingDate)
                .ToList();

            return View("Index", bookings);
        }

        // AC-01: View ticket details
        [HttpGet]
        public IActionResult Details(string id)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = _context.Invoices
                .Include(i => i.ScheduleSeats)
                    .ThenInclude(ss => ss.MovieShow)
                        .ThenInclude(ms => ms.CinemaRoom)
                .FirstOrDefault(i => i.InvoiceId == id && i.AccountId == accountId);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // AC-04: Cancel ticket
        [HttpPost]
        public IActionResult Cancel(string id)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = _context.Invoices
                .FirstOrDefault(i => i.InvoiceId == id && i.AccountId == accountId);

            if (booking == null)
            {
                return NotFound();
            }

            // Check if ticket can be canceled (24 hours before showtime)
            if (booking.ScheduleShow.HasValue && booking.ScheduleShow.Value.AddHours(-24) <= DateTime.Now)
            {
                TempData["Error"] = "Cannot cancel ticket within 24 hours of showtime.";
                return RedirectToAction(nameof(Index));
            }

            booking.Status = MovieTheater.Models.InvoiceStatus.Incomplete; // 0 = Canceled
            _context.SaveChanges();

            TempData["Success"] = "Ticket canceled successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult HistoryPartial(DateTime? fromDate, DateTime? toDate)
        {
            var accountId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return Content("<div class='alert alert-danger'>Not logged in.</div>", "text/html");

            var query = _context.Invoices.Where(i => i.AccountId == accountId);
            if (fromDate.HasValue)
                query = query.Where(i => i.BookingDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(i => i.BookingDate <= toDate.Value);
            var result = query.ToList();
            return PartialView("~/Views/Account/Tabs/_HistoryPartial.cshtml", result);
        }

        public IActionResult Test()
        {
            return Content("Test OK");
        }
    }
}