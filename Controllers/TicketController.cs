using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Repository;
using System.Threading.Tasks;

namespace MovieTheater.Controllers
{
    public class TicketController : Controller
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public TicketController(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }
        [HttpGet]
        public IActionResult History()
        {
            // Redirect /Ticket/History to /Ticket/Index
            return RedirectToAction("Index");
        }
        // AC-01: View all booked tickets
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = await _invoiceRepository.GetByAccountIdAsync(accountId);

            return View(bookings);
        }


        // AC-01: View booked tickets with filtering
        [HttpGet]
        public async Task<IActionResult> Booked()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = await _invoiceRepository.GetByAccountIdAsync(accountId, 1);

            return View("Index", bookings);
        }

        // AC-01: View canceled tickets
        [HttpGet]
        public async Task<IActionResult> Canceled()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = await _invoiceRepository.GetByAccountIdAsync(accountId, 0);

            return View("Index", bookings);
        }

        // AC-01: View ticket details
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = await _invoiceRepository.GetDetailsAsync(id, accountId);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // AC-04: Cancel ticket
        [HttpPost]
        public async Task<IActionResult> Cancel(string id)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = await _invoiceRepository.GetForCancelAsync(id, accountId);

            if (booking == null)
            {
                return NotFound();
            }

            // Check if ticket can be canceled (24 hours before showtime)
            if (
                booking.MovieShow.Schedule.ScheduleTime.HasValue &&
                booking.MovieShow.ShowDate.ToDateTime(booking.MovieShow.Schedule.ScheduleTime.Value).AddHours(-24) <= DateTime.Now
            )
            {
                TempData["Error"] = "Cannot cancel ticket within 24 hours of showtime.";
                return RedirectToAction(nameof(Index));
            }

            booking.Status = 0; // 0 = Canceled
            await _invoiceRepository.UpdateAsync(booking);

            TempData["Success"] = "Ticket canceled successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> HistoryPartial(System.DateTime? fromDate, System.DateTime? toDate)
        {
            var accountId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return Content("<div class='alert alert-danger'>Not logged in.</div>", "text/html");

            var result = await _invoiceRepository.GetByDateRangeAsync(accountId, fromDate, toDate);
            return PartialView("~/Views/Account/Tabs/_HistoryPartial.cshtml", result);
        }

        public IActionResult Test()
        {
            return Content("Test OK");
        }
    }
}