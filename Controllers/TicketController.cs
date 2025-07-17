using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MovieTheater.Models; // Added for Invoice and SeatDetailViewModel
using System.Linq;
using MovieTheater.Service; // Added for Sum

namespace MovieTheater.Controllers
{
    public class TicketController : Controller
    {
        private readonly ITicketService _ticketService;
        private readonly IFoodInvoiceService _foodInvoiceService; // Added for food invoice service

        public TicketController(ITicketService ticketService, IFoodInvoiceService foodInvoiceService)
        {
            _ticketService = ticketService;
            _foodInvoiceService = foodInvoiceService; // Initialize food invoice service
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

            var booking = await _ticketService.GetTicketDetailsAsync(id, accountId);
            if (booking == null)
                return NotFound();

            var seatDetails = _ticketService.BuildSeatDetails(booking);
            var selectedFoods = (await _foodInvoiceService.GetFoodsByInvoiceIdAsync(id)).ToList();
            var totalFoodPrice = selectedFoods.Sum(f => f.Price * f.Quantity);

            var viewModel = new MovieTheater.ViewModels.TicketDetailsViewModel
            {
                Booking = booking,
                SeatDetails = seatDetails,
                VoucherAmount = booking.Voucher?.Value,
                VoucherCode = booking.Voucher?.Code,
                TotalFoodPrice = totalFoodPrice,
                FoodDetails = selectedFoods
                // Nếu cần thêm trường khác, bổ sung ở đây
            };

            return View(viewModel);
        }

        // Xóa hoàn toàn method BuildSeatDetails khỏi controller

        [HttpPost]
        public async Task<IActionResult> Cancel(string id, string returnUrl)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction("Login", "Account");

            var (success, messages) = await _ticketService.CancelTicketAsync(id, accountId);
            TempData[success ? "ToastMessage" : "ErrorMessage"] = string.Join("<br/>", messages);

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