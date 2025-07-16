using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MovieTheater.Service;

namespace MovieTheater.Controllers
{
    public class TicketController : Controller
    {
        private readonly ITicketService _ticketService;
        private readonly IAccountService _accountService;

        public TicketController(ITicketService ticketService, IAccountService accountService)
        {
            _ticketService = ticketService;
            _accountService = accountService;
        }

        [HttpGet]
        public IActionResult History()
        {
            // Redirect /Ticket/History to /Ticket/Index
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction("Login", "Account");

            var bookings = await _ticketService.GetUserTicketsAsync(accountId);
            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Booked()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction("Login", "Account");

            var bookings = await _ticketService.GetUserTicketsAsync(accountId, 1); // 1 = Completed
            return View("Index", bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Canceled()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction("Login", "Account");

            var bookings = await _ticketService.GetUserTicketsAsync(accountId, 0); // 0 = Incomplete
            return View("Index", bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction("Login", "Account");

            var bookingDetails = await _ticketService.GetTicketDetailsAsync(id, accountId);
            if (bookingDetails == null)
                return NotFound();

            return View(bookingDetails);
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(string id, string returnUrl)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction("Login", "Account");

            var (success, messages) = await _ticketService.CancelTicketAsync(id, accountId);
            TempData[success ? "ToastMessage" : "ErrorMessage"] = string.Join("<br/>", messages);

            // Add rank change notification if any
            var rankUpMsg = _accountService.GetAndClearRankUpgradeNotification(accountId);
            if (!string.IsNullOrEmpty(rankUpMsg))
            {
                TempData["ToastMessage"] += "<br/>" + rankUpMsg;
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> HistoryPartial(DateTime? fromDate, DateTime? toDate, string status = "all")
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return Json(new { success = false, message = "Not logged in." });

            var result = await _ticketService.GetHistoryPartialAsync(accountId, fromDate, toDate, status);
            return Json(new { success = true, data = result });
        }

        public IActionResult Test()
        {
            return Content("Test OK");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelByAdmin(string id, string returnUrl)
        {
            var (success, messages) = await _ticketService.CancelTicketByAdminAsync(id);
            TempData[success ? "ToastMessage" : "ErrorMessage"] = string.Join("<br/>", messages);

            return RedirectToAction("TicketInfo", "Booking", new { invoiceId = id });
        }
    }
}